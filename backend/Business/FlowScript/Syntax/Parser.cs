using System.Globalization;

using Core.Enums;
using Core.Models.Database;
using Business.FlowScript.Diagnostics;

namespace Business.FlowScript.Syntax
{
    /// <summary>
    /// A .sflw file back into a flow, as far as text alone can take it.
    ///
    /// The mirror of <see cref="Printer"/> and pure like it: no database, no files, no
    /// ids. What the script says by name stays a name, and <see cref="FlowScriptImporter"/> is
    /// what turns those into rows.
    ///
    /// An unreadable line is recorded and skipped rather than thrown, so one typo reports one
    /// error instead of hiding the nine below it.
    /// </summary>
    public sealed class Parser : IParser
    {
        private enum Section
        {
            None,
            Areas,
            Points,
            Inputs,
            Steps,
        }

        public FlowSyntax Read(string script)
        {
            FlowSyntax document = new FlowSyntax();
            IReadOnlyList<ScriptLine> lines = Lexer.Read(script);

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
                        StepParser.Read(document, line, lastIndexAtIndent, pendingComments, ref order);
                        break;

                    default:
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(document.FlowName))
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.FLOW_LINE_MISSING, 1, 1, "The file has no \"Flow:\" line, so there is no flow to import."));

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
        private static void ReadHeaderField(FlowSyntax document, ScriptLine line)
        {
            int colon = line.Raw.IndexOf(':', StringComparison.Ordinal);
            if (colon < 0)
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.HEADER_MALFORMED, line.Number, 1, $"Expected a header line such as \"Flow:\", found \"{line.Raw.Trim()}\"."));
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
                        document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.ID_MALFORMED, line.Number, colon + 2, $"\"{value}\" is not an id. It should look like 8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111."));
                    break;

                case "Sizes":
                    ReadSizes(document, line, value, colon + 2);
                    break;

                default:
                    document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.HEADER_UNKNOWN, line.Number, 1, $"\"{key}\" is not something the header holds. Expected Flow, Id or Sizes."));
                    break;
            }
        }

        private static void ReadSizes(FlowSyntax document, ScriptLine line, string value, int column)
        {
            int order = 0;

            foreach (string size in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string[] parts = size.Split('x');

                if (parts.Length != 2
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height))
                {
                    document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.SIZE_MALFORMED, line.Number, column, $"\"{size}\" is not a size. Expected something like 1920x1080."));
                    continue;
                }

                document.Viewports.Add(new FlowViewport { Width = width, Height = height, OrderNumber = order++ });
            }
        }

        private static void ReadArea(FlowSyntax document, ScriptLine line)
        {
            if (line.Tokens.Count < 2 || !line.Tokens[0].WasQuoted)
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.AREA_NAME_MISSING, line.Number, 1, "An area starts with its name in quotes."));
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
                    document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.AREA_WINDOW_MALFORMED, line.Number, line.Tokens[1].Column, "Expected \"window process\" followed by the process name."));
                    return;
                }

                area.ProcessName = line.Word(3);

                if (line.Word(4) == "title")
                {
                    (TitleMatchModeEnum Mode, int Words)? match = SyntaxFacts.ReadTitleMatch(line.Tokens, 5);
                    if (match == null)
                    {
                        document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.TITLE_MATCH_UNKNOWN, line.Number, line.Tokens[4].Column, "Expected is, contains, starts with or matches after \"title\"."));
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
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.AREA_PLACEMENT_UNKNOWN, line.Number, line.Tokens.Count > 1 ? line.Tokens[1].Column : 1,
                    $"Expected \"window\" or \"inside\" after the area name, found \"{line.Word(1)}\"."));
                return;
            }

            document.Areas.Add(new AreaSyntax { Area = area, ParentName = parentName, Line = line.Number });
        }

        // ratio x y w h, or offset x y size w h
        private static bool ReadPlacement(FlowSyntax document, ScriptLine line, int at, FlowArea area)
        {
            if (line.Word(at) == "ratio")
            {
                area.SizingMode = AreaSizingModeEnum.RATIO;
                area.RatioX = SyntaxFacts.Float(line.Word(at + 1));
                area.RatioY = SyntaxFacts.Float(line.Word(at + 2));
                area.RatioWidth = SyntaxFacts.Float(line.Word(at + 3));
                area.RatioHeight = SyntaxFacts.Float(line.Word(at + 4));
                return true;
            }

            if (line.Word(at) == "offset" && line.Word(at + 3) == "size")
            {
                area.SizingMode = AreaSizingModeEnum.ABSOLUTE_PX;
                area.LocationX = SyntaxFacts.Integer(line.Word(at + 1));
                area.LocationY = SyntaxFacts.Integer(line.Word(at + 2));
                area.Width = SyntaxFacts.Integer(line.Word(at + 4));
                area.Height = SyntaxFacts.Integer(line.Word(at + 5));
                return true;
            }

            document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.PLACEMENT_MALFORMED, line.Number, at < line.Tokens.Count ? line.Tokens[at].Column : 1,
                "Expected \"ratio x y w h\" or \"offset x y size w h\"."));

            return false;
        }

        private static void ReadPoint(FlowSyntax document, ScriptLine line)
        {
            if (line.Tokens.Count < 2 || !line.Tokens[0].WasQuoted)
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
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.PLACEMENT_MALFORMED, line.Number, at < line.Tokens.Count ? line.Tokens[at].Column : 1,
                    "Expected \"ratio x y\" or \"offset x y\"."));
                return;
            }

            document.Points.Add(new PointSyntax { Point = point, AreaName = areaName, Line = line.Number });
        }

        private static void ReadInput(FlowSyntax document, ScriptLine line)
        {
            if (!line.Tokens[0].WasQuoted)
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.INPUT_MALFORMED, line.Number, 1, "An input is its name in quotes, optionally followed by \"secret\"."));
                return;
            }

            document.Inputs.Add(new FlowCsvColumn
            {
                Name = line.Tokens[0].Text,
                IsSecret = line.Word(1) == "secret",
                OrderNumber = document.Inputs.Count,
            });
        }

    }
}
