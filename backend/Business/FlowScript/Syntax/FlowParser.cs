using System.Drawing;

using Core.Enums;
using Core.Models.Database;

using Business.FlowScript.Catalogs;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models.Text;
using Business.FlowScript.Models.Binding;

namespace Business.FlowScript.Syntax
{
    /// <summary>
    /// Every Script line that contains a Flow information is parsed here.
    /// </summary>
    internal static class FlowParser
    {
        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Read Flow fields. The value is the rest of the line as written, not tokens.
        /// </summary>
        public static void ReadFlowField(FlowScriptSchema flowScriptSchema, ScriptLine line)
        {
            string flowLabel = SyntaxFacts.Keyword(ScriptSymbolEnum.FLOWFIELD_NAME);

            int colon = line.Raw.IndexOf(':', StringComparison.Ordinal); // Find the first colon possition in the line.
            if (colon < 0)
            {
                flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.HEADER_MALFORMED, line.Number, 1, $"Expected a header line such as \"{flowLabel}\", found \"{line.Raw.Trim()}\"."));
                return;
            }

            string key = line.Raw.Substring(0, colon + 1).Trim();
            string value = line.Raw.Substring(colon + 1).Trim();

            switch (SyntaxFacts.ReadSymbol(key))
            {
                case ScriptSymbolEnum.FLOWFIELD_NAME: // ex. "Flow:    Test: Login and add to cart"
                    if (SyntaxFacts.HasQuote(value))
                    {
                        flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.QUOTE_INSIDE, line.Number, colon + 2,
                            $"A flow's name can't contain \"{SyntaxFacts.Keyword(ScriptSymbolEnum.QUOTE_OPEN)}\" or \"{SyntaxFacts.Keyword(ScriptSymbolEnum.QUOTE_CLOSE)}\"."));
                        break;
                    }

                    flowScriptSchema.FlowName = value;
                    break;

                case ScriptSymbolEnum.FLOWFIELD_ID:  // ex. "Id:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111"
                    if (Guid.TryParse(value, out Guid id))
                        flowScriptSchema.PublicId = id;
                    else
                        flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.PUBLIC_ID_MALFORMED, line.Number, colon + 2, $"\"{value}\" is not an id. It should look like 8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111."));
                    break;

                case ScriptSymbolEnum.FLOWFIELD_SIZES: // ex. "Sizes:   1920x1080, 1024x768, 390x844"
                    ReadSizes(flowScriptSchema, line, value, colon + 2);
                    break;

                default:
                    string idLabel = SyntaxFacts.Keyword(ScriptSymbolEnum.FLOWFIELD_ID);
                    string sizesLabel = SyntaxFacts.Keyword(ScriptSymbolEnum.FLOWFIELD_SIZES);
                    flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.HEADER_UNKNOWN, line.Number, 1, $"\"{key}\" is not something the header holds. Expected \"{flowLabel}\", \"{idLabel}\" or \"{sizesLabel}\"."));
                    break;
            }
        }

        /// <summary>
        /// Read FlowArea fields.
        /// </summary>
        public static void ReadArea(FlowScriptSchema flowScriptSchema, ScriptLine line)
        {
            // ex. <[ Browser ]>   window process <[ chrome.exe ]> title contains <[ Swag Labs ]>   scales with dpi   at 120dpi
            ScriptLineReader reader = new ScriptLineReader(flowScriptSchema, line, 0);

            if (reader.Quoted(DiagnosticCodeEnum.AREA_NAME_MISSING, $"An area starts with its name in {Quotes()}.") is not string name)
                return;

            FlowArea area = new FlowArea { Name = name };
            string? parentName = null;

            if (reader.Take("window"))
            {
                area.Type = FlowAreaTypeEnum.APPLICATION;
                ReadWindow(reader, area);
            }
            else if (reader.Take("monitor"))
            {
                area.Type = FlowAreaTypeEnum.MONITOR;
                ReadMonitor(reader, area);
            }
            else if (reader.Is("on") && reader.PeekUnquoted(1) == "screen")
            {
                // Screen coordinates: correct on the machine it was made on and nowhere else.
                reader.Skip();
                reader.Skip();
                area.Type = FlowAreaTypeEnum.CUSTOM;
                ReadPlacement(reader, area);
            }
            else if (reader.Take("inside")) // ex. <[ Cart badge ]>    inside <[ Browser ]>   ratio 0.88 0.00  0.12 0.10
            {
                area.Type = FlowAreaTypeEnum.CUSTOM;
                parentName = reader.Quoted("the area it is inside");
                ReadPlacement(reader, area);
            }
            else
            {
                reader.Fail(DiagnosticCodeEnum.AREA_PLACEMENT_UNKNOWN, $"Expected \"window\", \"monitor\", \"on screen\" or \"inside\" after the area name, found {reader.Current}.");
            }

            ReadAreaScaling(reader, area);
            reader.End();

            if (reader.HasFailed)
                return;

            flowScriptSchema.Areas.Add(new FlowAreaSchemaBindng { Area = area, ParentName = parentName, Line = line.Number });
        }


        /// <summary>
        /// Read FlowPoint fields.
        /// </summary>
        public static void ReadPoint(FlowScriptSchema flowScriptSchema, ScriptLine line)
        {
            // <[ name ]>   inside <[ area ]> | on screen   ratio x y | offset x y   [at 120dpi]
            IReadOnlyList<ScriptWord> tokens = line.Words;

            if (!tokens[0].IsQuoted)
            {
                AddUnexpected(flowScriptSchema, line, 0);
                return;
            }

            FlowPointSchemaBindng result = new FlowPointSchemaBindng()
            {
                Point = new FlowPoint { Name = tokens[0].Value },
                Line = line.Number
            };

            int i = 1;

            // Measured from an area, or from the screen's corner - right here and nowhere else.
            ScriptKeyword? placement = SyntaxFacts.ReadKeyword<ScriptSymbolEnum>(tokens, i);
            if (placement?.As<ScriptSymbolEnum>() == ScriptSymbolEnum.INSIDE)
            {
                i += placement.TokenCount;
                if (tokens.ElementAtOrDefault(i)?.IsQuoted != true)
                {
                    AddUnexpected(flowScriptSchema, line, i);
                    return;
                }

                result.AreaName = tokens[i].Value;
                i++;
            }
            else if (placement?.As<ScriptSymbolEnum>() == ScriptSymbolEnum.ON_SCREEN)
            {
                i += placement.TokenCount;
            }
            else
            {
                AddUnexpected(flowScriptSchema, line, i);
                return;
            }

            // A fraction of the area, or pixels from its corner.
            ScriptKeyword? measure = SyntaxFacts.ReadKeyword<ScriptSymbolEnum>(tokens, i);
            if (measure?.As<ScriptSymbolEnum>() == ScriptSymbolEnum.RATIO)
            {
                i += measure.TokenCount;
                float? ratioX = SyntaxFacts.Float(line.ValueAt(i));
                float? ratioY = SyntaxFacts.Float(line.ValueAt(i + 1));
                if (ratioX == null || ratioY == null)
                {
                    AddUnexpected(flowScriptSchema, line, ratioX == null ? i : i + 1);
                    return;
                }

                result.Point.OffsetMode = AreaSizingModeEnum.RATIO;
                result.Point.RatioX = ratioX.Value;
                result.Point.RatioY = ratioY.Value;
                i += 2;
            }
            else if (measure?.As<ScriptSymbolEnum>() == ScriptSymbolEnum.OFFSET)
            {
                i += measure.TokenCount;
                int? locationX = SyntaxFacts.Integer(line.ValueAt(i));
                int? locationY = SyntaxFacts.Integer(line.ValueAt(i + 1));
                if (locationX == null || locationY == null)
                {
                    AddUnexpected(flowScriptSchema, line, locationX == null ? i : i + 1);
                    return;
                }

                result.Point.OffsetMode = AreaSizingModeEnum.ABSOLUTE_PX;
                result.Point.LocationX = locationX.Value;
                result.Point.LocationY = locationY.Value;
                i += 2;
            }
            else
            {
                AddUnexpected(flowScriptSchema, line, i);
                return;
            }

            // The DPI the offset was captured at. Optional: without it the pixels stay as written.
            ScriptKeyword? at = SyntaxFacts.ReadKeyword<ScriptSymbolEnum>(tokens, i);
            if (at?.As<ScriptSymbolEnum>() == ScriptSymbolEnum.AT)
            {
                i += at.TokenCount;
                int? dpi = SyntaxFacts.ReadDpi(line.ValueAt(i));
                if (dpi == null)
                {
                    AddUnexpected(flowScriptSchema, line, i);
                    return;
                }

                result.Point.AuthoredDpi = dpi.Value;
                i++;
            }

            // The line ends where the grammar does.
            if (i < tokens.Count)
            {
                AddUnexpected(flowScriptSchema, line, i);
                return;
            }

            flowScriptSchema.Points.Add(result);
        }

        /// <summary>
        /// Read FlowCsvColumn fields.
        /// </summary>
        public static void ReadCsvColumns(FlowScriptSchema flowScriptSchema, ScriptLine line)
        {
            // ex. <[ password ]>    secret
            ScriptLineReader reader = new ScriptLineReader(flowScriptSchema, line, 0);

            if (reader.Quoted(DiagnosticCodeEnum.CSV_COLUMN_MALFORMED, $"An input is its name in {Quotes()}, optionally followed by \"secret\".") is not string name)
                return;

            bool isSecret = reader.Take("secret");
            reader.End();

            if (reader.HasFailed)
                return;

            flowScriptSchema.Inputs.Add(new FlowCsvColumn
            {
                Name = name,
                IsSecret = isSecret,
                OrderNumber = flowScriptSchema.Inputs.Count,
            });
        }

        /// <summary>
        /// Read FlowStepTemplate fields.
        /// </summary>
        public static void ReadTemplate(FlowScriptSchema flowScriptSchema, ScriptLine line)
        {
            // ex. <[ a.png ]>   click 120,40   captured 800x600 at 120dpi
            ScriptLineReader reader = new ScriptLineReader(flowScriptSchema, line, 0);

            if (reader.Quoted(DiagnosticCodeEnum.TEMPLATE_MALFORMED, $"A template starts with its file name in {Quotes()}.") is not string fileName)
                return;

            if (flowScriptSchema.Templates.Any(x => string.Equals(x.FileName, fileName, StringComparison.Ordinal)))
            {
                flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.TEMPLATE_DUPLICATE, line.Number, 1, $"\"{fileName}\" is already described above."));
                return;
            }

            FlowStepTemplateSchemaBindng template = new FlowStepTemplateSchemaBindng { FileName = fileName, Line = line.Number };

            while (reader.HasMore)
            {
                if (reader.Take("click"))
                {
                    (int X, int Y)? click = SyntaxFacts.ReadPair(reader.PeekUnquoted(), ',');
                    if (click == null)
                    {
                        reader.Fail(DiagnosticCodeEnum.TEMPLATE_MALFORMED, $"{reader.Current} is not a click point. Expected something like 120,40.");
                        return;
                    }

                    template.ClickOffset = new Point(click.Value.X, click.Value.Y);
                }
                else if (reader.Take("captured"))
                {
                    (int Width, int Height)? size = SyntaxFacts.ReadPair(reader.PeekUnquoted(), 'x');
                    if (size == null)
                    {
                        reader.Fail(DiagnosticCodeEnum.TEMPLATE_MALFORMED, $"{reader.Current} is not a size. Expected something like 800x600.");
                        return;
                    }

                    template.AuthoredFlowAreaWidth = size.Value.Width;
                    template.AuthoredFlowAreaHeight = size.Value.Height;
                }
                else if (reader.Take("at"))
                {
                    int? dpi = SyntaxFacts.ReadDpi(reader.PeekUnquoted());
                    if (dpi == null)
                    {
                        reader.Fail(DiagnosticCodeEnum.TEMPLATE_MALFORMED, $"{reader.Current} is not a DPI. Expected something like 120dpi.");
                        return;
                    }

                    template.AuthoredDpi = dpi.Value;
                }
                else
                {
                    reader.Fail(DiagnosticCodeEnum.TEMPLATE_MALFORMED, $"{reader.Current} is not something a template takes. Expected click, captured or at.");
                    return;
                }

                reader.Skip();
            }

            flowScriptSchema.Templates.Add(template);
        }

        // ================================================================
        // Private methods - header
        // ================================================================

        private static void ReadSizes(FlowScriptSchema flowScriptSchema, ScriptLine line, string value, int column)
        {
            int order = 0;
            string[] sizes = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string size in sizes)
            {
                (int Width, int Height)? pair = SyntaxFacts.ReadPair(size, 'x');
                if (pair == null)
                {
                    flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.SIZE_MALFORMED, line.Number, column, $"\"{size}\" is not a size. Expected something like 1920x1080."));
                    continue;
                }

                flowScriptSchema.Viewports.Add(new FlowViewport { Width = pair.Value.Width, Height = pair.Value.Height, OrderNumber = order++ });
            }
        }

        // ================================================================
        // Private methods - areas
        // ================================================================

        // window process <[ x ]>, optionally title ... <[ y ]>.
        private static void ReadWindow(ScriptLineReader reader, FlowArea area)
        {
            if (!reader.Expect("process", DiagnosticCodeEnum.AREA_WINDOW_MALFORMED, "Expected \"window process\" followed by the process name."))
                return;

            if (reader.Quoted("the process name") is not string process)
                return;

            area.ProcessName = process;

            if (!reader.Take("title"))
                return;

            ScriptKeyword? match = reader.Keyword<TitleMatchModeEnum>();
            if (match == null)
            {
                reader.Fail(DiagnosticCodeEnum.TITLE_MATCH_UNKNOWN, "Expected is, contains, starts with or matches after \"title\".");
                return;
            }

            area.TitleMatchMode = match.As<TitleMatchModeEnum>()!.Value;
            if (reader.Quoted("the title to match") is string title)
                area.TitlePattern = title;
        }

        // monitor primary, or monitor <[ \\.\DISPLAY2 ]>. Quoted, because a device could be called primary.
        private static void ReadMonitor(ScriptLineReader reader, FlowArea area)
        {
            if (reader.Take("primary"))
            {
                area.MonitorDeviceName = string.Empty;
                return;
            }

            if (reader.Quoted(DiagnosticCodeEnum.PLACEMENT_MALFORMED, $"Expected \"monitor primary\" or \"monitor\" and the device name in {Quotes()}.") is string device)
                area.MonitorDeviceName = device;
        }

        // ratio x y w h, or offset x y size w h.
        private static void ReadPlacement(ScriptLineReader reader, FlowArea area)
        {
            if (reader.Take("ratio"))
            {
                if (reader.Float("the x ratio") is float x
                    && reader.Float("the y ratio") is float y
                    && reader.Float("the width ratio") is float width
                    && reader.Float("the height ratio") is float height)
                {
                    area.SizingMode = AreaSizingModeEnum.RATIO;
                    area.RatioX = x;
                    area.RatioY = y;
                    area.RatioWidth = width;
                    area.RatioHeight = height;
                }

                return;
            }

            if (reader.Take("offset"))
            {
                if (reader.Integer("the x offset") is int x
                    && reader.Integer("the y offset") is int y
                    && reader.Expect("size", DiagnosticCodeEnum.PLACEMENT_MALFORMED, "Expected \"size\" and the width and height after the offset.")
                    && reader.Integer("the width") is int width
                    && reader.Integer("the height") is int height)
                {
                    area.SizingMode = AreaSizingModeEnum.ABSOLUTE_PX;
                    area.LocationX = x;
                    area.LocationY = y;
                    area.Width = width;
                    area.Height = height;
                }

                return;
            }

            reader.Fail(DiagnosticCodeEnum.PLACEMENT_MALFORMED, "Expected \"ratio x y w h\" or \"offset x y size w h\".");
        }

        // scales with dpi|area, at 120dpi. Both optional: no setting inherits, no DPI leaves pixels as they are.
        private static void ReadAreaScaling(ScriptLineReader reader, FlowArea area)
        {
            while (reader.HasMore)
            {
                if (reader.Take("scales"))
                {
                    ScalesWithEnum? scalesWith = null;
                    if (reader.Take("with"))
                        scalesWith = SyntaxFacts.ReadScalesWith(reader.PeekUnquoted());

                    if (scalesWith == null)
                    {
                        reader.Fail(DiagnosticCodeEnum.AREA_ARGUMENT_UNKNOWN, "Expected \"scales with dpi\" or \"scales with area\".");
                        return;
                    }

                    area.ScalesWith = scalesWith.Value;
                    reader.Skip();
                }
                else if (reader.Take("at"))
                {
                    int? dpi = SyntaxFacts.ReadDpi(reader.PeekUnquoted());
                    if (dpi == null)
                    {
                        reader.Fail(DiagnosticCodeEnum.AREA_ARGUMENT_UNKNOWN, $"{reader.Current} is not a DPI. Expected something like 120dpi.");
                        return;
                    }

                    area.AuthoredDpi = dpi.Value;
                    reader.Skip();
                }
                else
                {
                    reader.Fail(DiagnosticCodeEnum.AREA_ARGUMENT_UNKNOWN, $"{reader.Current} is not something an area takes.");
                    return;
                }
            }
        }

        private static string Quotes()
        {
            return $"{SyntaxFacts.Keyword(ScriptSymbolEnum.QUOTE_OPEN)} {SyntaxFacts.Keyword(ScriptSymbolEnum.QUOTE_CLOSE)}";
        }

        // Unexpected "offset" - at that token, or just past the end of the line when nothing is there.
        private static void AddUnexpected(FlowScriptSchema flowScriptSchema, ScriptLine line, int index)
        {
            string message = "Unexpected end of the line.";
            if (index < line.Words.Count)
                message = $"Unexpected \"{line.ValueAt(index)}\".";

            flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.TOKEN_UNEXPECTED, line.Number, line.ColumnOf(index), message));
        }
    }
}
