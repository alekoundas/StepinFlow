using System.Drawing;
using System.Globalization;

using Core.Enums;
using Core.Models.Database;

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
        /// Read Flow fields
        /// </summary>
        public static void ReadFlowField(FlowScriptSchema document, ScriptLine line)
        {
            int colon = line.Raw.IndexOf(':', StringComparison.Ordinal); // Find the first colon possition in the line.
            if (colon < 0)
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.HEADER_MALFORMED, line.Number, 1, $"Expected a header line such as \"Flow:\", found \"{line.Raw.Trim()}\"."));
                return;
            }

            string key = line.Raw[..colon].Trim();
            string value = line.Raw[(colon + 1)..].Trim();

            switch (key)
            {
                case "Flow": // ex. "Flow:    Test: Login and add to cart"
                    document.FlowName = value;
                    break;

                case "Id":  // ex. "Id:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111"
                    if (Guid.TryParse(value, out Guid id))
                        document.PublicId = id;
                    else
                        document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.PUBLIC_ID_MALFORMED, line.Number, colon + 2, $"\"{value}\" is not an id. It should look like 8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111."));
                    break;

                case "Sizes": // ex. "Sizes:   1920x1080, 1024x768, 390x844"
                    ReadSizes(document, line, value, colon + 2);
                    break;

                default:
                    document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.HEADER_UNKNOWN, line.Number, 1, $"\"{key}\" is not something the header holds. Expected Flow, Id or Sizes."));
                    break;
            }
        }

        /// <summary>
        /// Read FlowArea fields.
        /// </summary>
        public static void ReadArea(FlowScriptSchema document, ScriptLine line)
        {
            if (line.Tokens.Count < 2 || !line.Tokens[0].IsQuoted)
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.AREA_NAME_MISSING, line.Number, 1, "An area starts with its name in quotes."));
                return;
            }

            FlowArea area = new FlowArea { Name = line.Tokens[0].Text };
            string? parentName = null;
            int next;

            if (line.Word(1) == "window") // ex.   ""Browser"       window process "chrome.exe" title contains "Swag Labs"   scales with dpi   at 120dpi"
            {
                area.Type = FlowAreaTypeEnum.APPLICATION;
                next = ReadWindow(document, line, area);
            }
            else if (line.Word(1) == "monitor")
            {
                area.Type = FlowAreaTypeEnum.MONITOR;
                next = ReadMonitor(document, line, area);
            }
            else if (line.Word(1) == "on" && line.Word(2) == "screen")
            {
                // Screen coordinates: correct on the machine it was made on and nowhere else.
                area.Type = FlowAreaTypeEnum.CUSTOM;
                next = ReadPlacement(document, line, 3, area);
            }
            else if (line.Word(1) == "inside") // ex. ""Cart badge"    inside "Browser"   ratio 0.88 0.00  0.12 0.10"
            {
                area.Type = FlowAreaTypeEnum.CUSTOM;
                parentName = line.Word(2);
                next = ReadPlacement(document, line, 3, area);
            }
            else
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.AREA_PLACEMENT_UNKNOWN, line.Number, line.ColumnOf(1),
                    $"Expected \"window\", \"monitor\", \"on screen\" or \"inside\" after the area name, found \"{line.Word(1)}\"."));
                return;
            }

            if (next < 0 || !ReadAreaScaling(document, line, next, area))
                return;

            document.Areas.Add(new FlowAreaSchemaBindng { Area = area, ParentName = parentName, Line = line.Number });
        }


        /// <summary>
        /// Read FlowPoint fields.
        /// </summary>
        public static void ReadPoint(FlowScriptSchema document, ScriptLine line)
        {
            if (line.Tokens.Count < 2 || !line.Tokens[0].IsQuoted)
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.POINT_NAME_MISSING, line.Number, 1, "A point starts with its name in quotes."));
                return;
            }

            FlowPoint point = new FlowPoint { Name = line.Tokens[0].Text };
            string? areaName = null;
            int at;

            if (line.Word(1) == "inside")
            {
                areaName = line.Word(2);
                at = 3;
            }
            else if (line.Word(1) == "on" && line.Word(2) == "screen")
            {
                at = 3;
            }
            else
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.POINT_PLACEMENT_UNKNOWN, line.Number, line.Tokens[1].Column, "Expected \"inside\" or \"on screen\" after the point name."));
                return;
            }

            if (line.Word(at) == "ratio")
            {
                point.OffsetMode = AreaSizingModeEnum.RATIO;
                point.RatioX = SyntaxFacts.Float(line.Word(at + 1));
                point.RatioY = SyntaxFacts.Float(line.Word(at + 2));
            }
            else if (line.Word(at) == "offset")
            {
                point.OffsetMode = AreaSizingModeEnum.ABSOLUTE_PX;
                point.LocationX = SyntaxFacts.Integer(line.Word(at + 1));
                point.LocationY = SyntaxFacts.Integer(line.Word(at + 2));
            }
            else
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.PLACEMENT_MALFORMED, line.Number, line.ColumnOf(at),
                    "Expected \"ratio x y\" or \"offset x y\"."));
                return;
            }

            // at 120dpi: optional, and without it the pixels stay as written.
            int next = at + 3;
            if (line.Word(next) == "at")
            {
                int? dpi = SyntaxFacts.ReadDpi(line.Word(next + 1));
                if (dpi == null)
                {
                    document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.POINT_ARGUMENT_UNKNOWN, line.Number, line.ColumnOf(next + 1), $"\"{line.Word(next + 1)}\" is not a DPI. Expected something like 120dpi."));
                    return;
                }

                point.AuthoredDpi = dpi.Value;
                next += 2;
            }

            if (next < line.Tokens.Count)
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.POINT_ARGUMENT_UNKNOWN, line.Number, line.ColumnOf(next), $"\"{line.Word(next)}\" is not something a point takes."));
                return;
            }

            document.Points.Add(new FlowPointSchemaBindng { Point = point, AreaName = areaName, Line = line.Number });
        }

        public static void ReadCsvColumns(FlowScriptSchema document, ScriptLine line)
        {
            if (!line.Tokens[0].IsQuoted)
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.CSV_COLUMN_MALFORMED, line.Number, 1, "An input is its name in quotes, optionally followed by \"secret\"."));
                return;
            }

            document.Inputs.Add(new FlowCsvColumn
            {
                Name = line.Tokens[0].Text,
                IsSecret = line.Word(1) == "secret",
                OrderNumber = document.Inputs.Count,
            });
        }

        /// <summary>
        /// Read FlowStepTemplate fields.
        /// ex. "a.png"   click 120,40   captured 800x600 at 120dpi".
        /// </summary>
        public static void ReadTemplate(FlowScriptSchema document, ScriptLine line)
        {
            if (!line.Tokens[0].IsQuoted)
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.TEMPLATE_MALFORMED, line.Number, 1, "A template starts with its file name in quotes."));
                return;
            }

            string fileName = line.Tokens[0].Text;
            if (document.Templates.Any(x => string.Equals(x.FileName, fileName, StringComparison.Ordinal)))
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.TEMPLATE_DUPLICATE, line.Number, 1, $"\"{fileName}\" is already described above."));
                return;
            }

            FlowStepTemplateSchemaBindng template = new FlowStepTemplateSchemaBindng { FileName = fileName, Line = line.Number };
            int i = 1;

            while (i < line.Tokens.Count)
            {
                string word = line.Word(i);
                string value = line.Word(i + 1);

                switch (word)
                {
                    case "click":
                        (int X, int Y)? click = SyntaxFacts.ReadPair(value, ',');
                        if (click == null)
                        {
                            document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.TEMPLATE_MALFORMED, line.Number, line.ColumnOf(i + 1), $"\"{value}\" is not a click point. Expected something like 120,40."));
                            return;
                        }

                        template.ClickOffset = new Point(click.Value.X, click.Value.Y);
                        break;

                    case "captured":
                        (int Width, int Height)? size = SyntaxFacts.ReadPair(value, 'x');
                        if (size == null)
                        {
                            document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.TEMPLATE_MALFORMED, line.Number, line.ColumnOf(i + 1), $"\"{value}\" is not a size. Expected something like 800x600."));
                            return;
                        }

                        template.AuthoredFlowAreaWidth = size.Value.Width;
                        template.AuthoredFlowAreaHeight = size.Value.Height;
                        break;

                    case "at":
                        int? dpi = SyntaxFacts.ReadDpi(value);
                        if (dpi == null)
                        {
                            document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.TEMPLATE_MALFORMED, line.Number, line.ColumnOf(i + 1), $"\"{value}\" is not a DPI. Expected something like 120dpi."));
                            return;
                        }

                        template.AuthoredDpi = dpi.Value;
                        break;

                    default:
                        document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.TEMPLATE_MALFORMED, line.Number, line.ColumnOf(i), $"\"{word}\" is not something a template takes. Expected click, captured or at."));
                        return;
                }

                i += 2;
            }

            document.Templates.Add(template);
        }

        // ================================================================
        // Private methods - header
        // ================================================================

        private static void ReadSizes(FlowScriptSchema document, ScriptLine line, string value, int column)
        {
            int order = 0;
            string[] sizes = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string size in sizes)
            {
                string[] parts = size.Split('x');
                bool isWidthValid = int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width);
                bool isHeightValid = int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height);

                if (parts.Length != 2 || !isWidthValid|| !isHeightValid)
                {
                    document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.SIZE_MALFORMED, line.Number, column, $"\"{size}\" is not a size. Expected something like 1920x1080."));
                    continue;
                }

                document.Viewports.Add(new FlowViewport { Width = width, Height = height, OrderNumber = order++ });
            }
        }

        // ================================================================
        // Private methods - areas
        // ================================================================

        // window process "x", optionally title ... "y". Where the scaling clauses start, or -1.
        private static int ReadWindow(FlowScriptSchema document, ScriptLine line, FlowArea area)
        {
            if (line.Word(2) != "process")
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.AREA_WINDOW_MALFORMED, line.Number, line.ColumnOf(1), "Expected \"window process\" followed by the process name."));
                return -1;
            }

            area.ProcessName = line.Word(3);

            if (line.Word(4) != "title")
                return 4;

            (TitleMatchModeEnum Mode, int Words)? match = SyntaxFacts.ReadTitleMatch(line.Tokens, 5);
            if (match == null)
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.TITLE_MATCH_UNKNOWN, line.Number, line.ColumnOf(4), "Expected is, contains, starts with or matches after \"title\"."));
                return -1;
            }

            area.TitleMatchMode = match.Value.Mode;
            area.TitlePattern = line.Word(5 + match.Value.Words);

            return 6 + match.Value.Words;
        }

        // monitor primary, or monitor "\\.\DISPLAY2". Quoted, because a device could be called primary.
        private static int ReadMonitor(FlowScriptSchema document, ScriptLine line, FlowArea area)
        {
            if (line.Word(2) == "primary" && !line.Tokens[2].IsQuoted)
            {
                area.MonitorDeviceName = string.Empty;
                return 3;
            }

            if (line.Tokens.Count > 2 && line.Tokens[2].IsQuoted)
            {
                area.MonitorDeviceName = line.Word(2);
                return 3;
            }

            document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.PLACEMENT_MALFORMED, line.Number, line.ColumnOf(2), "Expected \"monitor primary\" or \"monitor\" and the device name in quotes."));
            return -1;
        }

        // ratio x y w h, or offset x y size w h. Where the scaling clauses start, or -1.
        private static int ReadPlacement(FlowScriptSchema document, ScriptLine line, int at, FlowArea area)
        {
            if (line.Word(at) == "ratio")
            {
                area.SizingMode = AreaSizingModeEnum.RATIO;
                area.RatioX = SyntaxFacts.Float(line.Word(at + 1));
                area.RatioY = SyntaxFacts.Float(line.Word(at + 2));
                area.RatioWidth = SyntaxFacts.Float(line.Word(at + 3));
                area.RatioHeight = SyntaxFacts.Float(line.Word(at + 4));
                return at + 5;
            }

            if (line.Word(at) == "offset" && line.Word(at + 3) == "size")
            {
                area.SizingMode = AreaSizingModeEnum.ABSOLUTE_PX;
                area.LocationX = SyntaxFacts.Integer(line.Word(at + 1));
                area.LocationY = SyntaxFacts.Integer(line.Word(at + 2));
                area.Width = SyntaxFacts.Integer(line.Word(at + 4));
                area.Height = SyntaxFacts.Integer(line.Word(at + 5));
                return at + 6;
            }

            document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.PLACEMENT_MALFORMED, line.Number, line.ColumnOf(at),
                "Expected \"ratio x y w h\" or \"offset x y size w h\"."));

            return -1;
        }

        // scales with dpi|area, at 120dpi. Both optional: no setting inherits, no DPI leaves pixels as they are.
        // ex. " "Browser"       window process "chrome.exe" title contains "Swag Labs"   scales with dpi   at 120dpi"
        private static bool ReadAreaScaling(FlowScriptSchema document, ScriptLine line, int at, FlowArea area)
        {
            int i = at;

            while (i < line.Tokens.Count)
            {
                string word = line.Word(i);

                switch (word)
                {
                    case "scales":
                        ScalesWithEnum? scalesWith = null;
                        if (line.Word(i + 1) == "with")
                            scalesWith = SyntaxFacts.ReadScalesWith(line.Word(i + 2));

                        if (scalesWith == null)
                        {
                            document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.AREA_ARGUMENT_UNKNOWN, line.Number, line.ColumnOf(i), "Expected \"scales with dpi\" or \"scales with area\"."));
                            return false;
                        }

                        area.ScalesWith = scalesWith.Value;
                        i += 3;
                        break;

                    case "at":
                        int? dpi = SyntaxFacts.ReadDpi(line.Word(i + 1));
                        if (dpi == null)
                        {
                            document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.AREA_ARGUMENT_UNKNOWN, line.Number, line.ColumnOf(i + 1), $"\"{line.Word(i + 1)}\" is not a DPI. Expected something like 120dpi."));
                            return false;
                        }

                        area.AuthoredDpi = dpi.Value;
                        i += 2;
                        break;

                    default:
                        document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.AREA_ARGUMENT_UNKNOWN, line.Number, line.ColumnOf(i), $"\"{word}\" is not something an area takes."));
                        return false;
                }
            }

            return true;
        }
    }
}
