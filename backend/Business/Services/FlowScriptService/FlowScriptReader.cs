using System.Globalization;

using Core.Enums;
using Core.Models.Database;

namespace Business.Services.FlowScriptService
{
    /// <summary>
    /// A .sflw file back into a flow, as far as text alone can take it.
    ///
    /// The mirror of <see cref="FlowScriptWriter"/> and pure like it: no database, no files, no
    /// ids. What the script says by name stays a name, and <see cref="FlowScriptImporter"/> is
    /// what turns those into rows.
    ///
    /// An unreadable line is recorded and skipped rather than thrown, so one typo reports one
    /// error instead of hiding the nine below it.
    /// </summary>
    public sealed partial class FlowScriptReader : IFlowScriptReader
    {
        private enum Section
        {
            None,
            Areas,
            Points,
            Inputs,
            Steps,
        }

        public FlowScriptDocument Read(string script)
        {
            FlowScriptDocument document = new FlowScriptDocument();
            IReadOnlyList<ScriptLine> lines = ScriptTokenizer.Read(script);

            Section section = Section.None;
            Dictionary<int, int> lastIndexAtIndent = new Dictionary<int, int>();
            List<string> pendingComments = new List<string>();
            int order = 0;

            foreach (ScriptLine line in lines)
            {
                if (line.IsBlank)
                    continue;

                if (line.IsComment)
                {
                    pendingComments.Add(line.TextAfterHash);
                    continue;
                }

                Section? opened = OpensSection(line);
                if (opened != null)
                {
                    section = opened.Value;
                    continue;
                }

                if (section == Section.None)
                {
                    ReadHeaderField(document, line);
                    continue;
                }

                switch (section)
                {
                    case Section.Areas:
                        ReadArea(document, line);
                        break;

                    case Section.Points:
                        ReadPoint(document, line);
                        break;

                    case Section.Inputs:
                        ReadInput(document, line);
                        break;

                    case Section.Steps:
                        ReadStep(document, line, lastIndexAtIndent, pendingComments, ref order);
                        break;

                    default:
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(document.FlowName))
                document.Errors.Add(new FlowScriptError(1, 1, "The file has no \"Flow:\" line, so there is no flow to import."));

            return document;
        }


        // ================================================================
        // Private methods - header
        // ================================================================

        private static Section? OpensSection(ScriptLine line)
        {
            switch (line.Raw.Trim())
            {
                case "Areas:": return Section.Areas;
                case "Points:": return Section.Points;
                case "Inputs:": return Section.Inputs;
                case "Steps:": return Section.Steps;
                default: return null;
            }
        }

        // Flow and Id are written unquoted, so the value is the rest of the line rather than a token.
        private static void ReadHeaderField(FlowScriptDocument document, ScriptLine line)
        {
            int colon = line.Raw.IndexOf(':', StringComparison.Ordinal);
            if (colon < 0)
            {
                document.Errors.Add(new FlowScriptError(line.Number, 1, $"Expected a header line such as \"Flow:\", found \"{line.Raw.Trim()}\"."));
                return;
            }

            string key = line.Raw[..colon].Trim();
            string value = line.Raw[(colon + 1)..].Trim();

            switch (key)
            {
                case "Flow":
                    document.FlowName = value;
                    break;

                case "Id":
                    if (Guid.TryParse(value, out Guid id))
                        document.PublicId = id;
                    else
                        document.Errors.Add(new FlowScriptError(line.Number, colon + 2, $"\"{value}\" is not an id. It should look like 8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111."));
                    break;

                case "Sizes":
                    ReadSizes(document, line, value, colon + 2);
                    break;

                default:
                    document.Errors.Add(new FlowScriptError(line.Number, 1, $"\"{key}\" is not something the header holds. Expected Flow, Id or Sizes."));
                    break;
            }
        }

        private static void ReadSizes(FlowScriptDocument document, ScriptLine line, string value, int column)
        {
            int order = 0;

            foreach (string size in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string[] parts = size.Split('x');

                if (parts.Length != 2
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height))
                {
                    document.Errors.Add(new FlowScriptError(line.Number, column, $"\"{size}\" is not a size. Expected something like 1920x1080."));
                    continue;
                }

                document.Viewports.Add(new FlowViewport { Width = width, Height = height, OrderNumber = order++ });
            }
        }

        private static void ReadArea(FlowScriptDocument document, ScriptLine line)
        {
            if (line.Tokens.Count < 2 || !line.Tokens[0].WasQuoted)
            {
                document.Errors.Add(new FlowScriptError(line.Number, 1, "An area starts with its name in quotes."));
                return;
            }

            FlowArea area = new FlowArea { Name = line.Tokens[0].Text };
            string? parentName = null;

            if (line.Word(1) == "window")
            {
                // A root area binds to a window: process, and optionally a title.
                area.Type = FlowAreaTypeEnum.APPLICATION;

                if (line.Word(2) != "process")
                {
                    document.Errors.Add(new FlowScriptError(line.Number, line.Tokens[1].Column, "Expected \"window process\" followed by the process name."));
                    return;
                }

                area.ProcessName = line.Word(3);

                if (line.Word(4) == "title")
                {
                    (TitleMatchModeEnum Mode, int Words)? match = Words.ReadTitleMatch(line.Tokens, 5);
                    if (match == null)
                    {
                        document.Errors.Add(new FlowScriptError(line.Number, line.Tokens[4].Column, "Expected is, contains, starts with or matches after \"title\"."));
                        return;
                    }

                    area.TitleMatchMode = match.Value.Mode;
                    area.TitlePattern = line.Word(5 + match.Value.Words);
                }
            }
            else if (line.Word(1) == "inside")
            {
                area.Type = FlowAreaTypeEnum.CUSTOM;
                parentName = line.Word(2);

                if (!ReadPlacement(document, line, 3, area))
                    return;
            }
            else
            {
                document.Errors.Add(new FlowScriptError(line.Number, line.Tokens.Count > 1 ? line.Tokens[1].Column : 1,
                    $"Expected \"window\" or \"inside\" after the area name, found \"{line.Word(1)}\"."));
                return;
            }

            document.Areas.Add(new ParsedArea { Area = area, ParentName = parentName, Line = line.Number });
        }

        // ratio x y w h, or offset x y size w h
        private static bool ReadPlacement(FlowScriptDocument document, ScriptLine line, int at, FlowArea area)
        {
            if (line.Word(at) == "ratio")
            {
                area.SizingMode = AreaSizingModeEnum.RATIO;
                area.RatioX = Float(line.Word(at + 1));
                area.RatioY = Float(line.Word(at + 2));
                area.RatioWidth = Float(line.Word(at + 3));
                area.RatioHeight = Float(line.Word(at + 4));
                return true;
            }

            if (line.Word(at) == "offset" && line.Word(at + 3) == "size")
            {
                area.SizingMode = AreaSizingModeEnum.ABSOLUTE_PX;
                area.LocationX = Integer(line.Word(at + 1));
                area.LocationY = Integer(line.Word(at + 2));
                area.Width = Integer(line.Word(at + 4));
                area.Height = Integer(line.Word(at + 5));
                return true;
            }

            document.Errors.Add(new FlowScriptError(line.Number, at < line.Tokens.Count ? line.Tokens[at].Column : 1,
                "Expected \"ratio x y w h\" or \"offset x y size w h\"."));

            return false;
        }

        private static void ReadPoint(FlowScriptDocument document, ScriptLine line)
        {
            if (line.Tokens.Count < 2 || !line.Tokens[0].WasQuoted)
            {
                document.Errors.Add(new FlowScriptError(line.Number, 1, "A point starts with its name in quotes."));
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
                document.Errors.Add(new FlowScriptError(line.Number, line.Tokens[1].Column, "Expected \"inside\" or \"on screen\" after the point name."));
                return;
            }

            if (line.Word(at) == "ratio")
            {
                point.OffsetMode = AreaSizingModeEnum.RATIO;
                point.RatioX = Float(line.Word(at + 1));
                point.RatioY = Float(line.Word(at + 2));
            }
            else if (line.Word(at) == "offset")
            {
                point.OffsetMode = AreaSizingModeEnum.ABSOLUTE_PX;
                point.LocationX = Integer(line.Word(at + 1));
                point.LocationY = Integer(line.Word(at + 2));
            }
            else
            {
                document.Errors.Add(new FlowScriptError(line.Number, at < line.Tokens.Count ? line.Tokens[at].Column : 1,
                    "Expected \"ratio x y\" or \"offset x y\"."));
                return;
            }

            document.Points.Add(new ParsedPoint { Point = point, AreaName = areaName, Line = line.Number });
        }

        private static void ReadInput(FlowScriptDocument document, ScriptLine line)
        {
            if (!line.Tokens[0].WasQuoted)
            {
                document.Errors.Add(new FlowScriptError(line.Number, 1, "An input is its name in quotes, optionally followed by \"secret\"."));
                return;
            }

            document.Inputs.Add(new FlowCsvColumn
            {
                Name = line.Tokens[0].Text,
                IsSecret = line.Word(1) == "secret",
                OrderNumber = document.Inputs.Count,
            });
        }


        // ================================================================
        // Private methods - numbers
        // ================================================================

        private static float Float(string text)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;
        }

        private static int Integer(string text)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
        }
    }
}
