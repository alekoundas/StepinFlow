using System.Globalization;
using System.Text;

using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Business.FlowScript.Binding;
using Business.FlowScript.Syntax;

namespace Business.FlowScript.Text
{
    /// <summary>
    /// A flow as the text that goes in a repository.
    ///
    /// See FLOW-FORMAT.md
    ///
    /// Deterministic.
    /// </summary>
    public sealed class Printer : IPrinter
    {
        private const int IndentWidth = 2;

        public string Write(BoundFlow source)
        {
            StringBuilder builder = new StringBuilder();

            WriteHeader(builder, source);
            WriteSteps(builder, source);

            return builder.ToString();
        }


        // ================================================================
        // Private methods - header
        // ================================================================

        private static void WriteHeader(StringBuilder builder, BoundFlow source)
        {
            builder.Append("Flow:    ").AppendLine(source.Flow.Name);
            builder.Append("Id:      ").AppendLine(source.Flow.PublicId.ToString());

            if (source.Viewports.Count > 0)
            {
                IEnumerable<string> sizes = source.Viewports
                    .OrderBy(x => x.OrderNumber)
                    .Select(x => $"{x.Width}x{x.Height}");

                builder.Append("Sizes:   ").AppendLine(string.Join(", ", sizes));
            }

            builder.AppendLine();

            WriteAreas(builder, source);
            WritePoints(builder, source);
            WriteInputs(builder, source);
            WriteTemplates(builder, source);
        }

        private static void WriteAreas(StringBuilder builder, BoundFlow source)
        {
            if (source.Areas.Count == 0)
                return;

            builder.AppendLine("Areas:");

            // Parents before children, so a child's "inside X" always names something already read.
            foreach (FlowArea area in Ordered(source.Areas))
                builder.Append("  ").AppendLine(AreaLine(area, source));

            builder.AppendLine();
        }

        private static string AreaLine(FlowArea area, BoundFlow source)
        {
            string name = Pad(Quoted(area.Name), 16);

            return $"{name}{AreaPlacement(area, source)}{AreaScaling(area)}";
        }

        private static string AreaPlacement(FlowArea area, BoundFlow source)
        {
            if (area.ParentFlowAreaId != null)
            {
                string parent = Quoted(source.AreaNamesById.GetValueOrDefault(area.ParentFlowAreaId.Value, string.Empty));

                return $"inside {parent}   {AreaSize(area)}";
            }

            switch (area.Type)
            {
                // Empty is the primary monitor, and "primary" is unquoted so a device cannot be mistaken for it.
                case FlowAreaTypeEnum.MONITOR:
                    if (area.MonitorDeviceName.Length == 0)
                        return "monitor primary";

                    return $"monitor {Quoted(area.MonitorDeviceName)}";

                case FlowAreaTypeEnum.CUSTOM:
                    return $"on screen   {AreaSize(area)}";

                default:
                    string title = string.Empty;
                    if (!string.IsNullOrWhiteSpace(area.TitlePattern))
                        title = $" title {SyntaxFacts.TitleMatch(area.TitleMatchMode)} {Quoted(area.TitlePattern)}";

                    return $"window process {Quoted(area.ProcessName)}{title}";
            }
        }

        private static string AreaSize(FlowArea area)
        {
            if (area.SizingMode == AreaSizingModeEnum.RATIO)
                return $"ratio {Ratio(area.RatioX)} {Ratio(area.RatioY)}  {Ratio(area.RatioWidth)} {Ratio(area.RatioHeight)}";

            return $"offset {Integer(area.LocationX)} {Integer(area.LocationY)}  size {Integer(area.Width)} {Integer(area.Height)}";
        }

        // Left out when unset: no setting inherits the parent's, and no DPI leaves pixels as they are.
        private static string AreaScaling(FlowArea area)
        {
            string text = string.Empty;

            if (area.ScalesWith != null)
                text += $"   scales with {SyntaxFacts.ScalesWith(area.ScalesWith.Value)}";

            if (area.AuthoredDpi > 0)
                text += $"   at {Dpi(area.AuthoredDpi)}";

            return text;
        }

        private static void WritePoints(StringBuilder builder, BoundFlow source)
        {
            if (source.Points.Count == 0)
                return;

            builder.AppendLine("Points:");

            foreach (FlowPoint point in source.Points.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                string name = Pad(Quoted(point.Name), 16);

                string placement = $"offset {Integer(point.LocationX)} {Integer(point.LocationY)}";
                if (point.OffsetMode == AreaSizingModeEnum.RATIO)
                    placement = $"ratio {Ratio(point.RatioX)} {Ratio(point.RatioY)}";

                string inside = "on screen";
                if (point.FlowAreaId != null)
                    inside = $"inside {Quoted(source.AreaNamesById.GetValueOrDefault(point.FlowAreaId.Value, string.Empty))}";

                builder.Append("  ").AppendLine(CultureInfo.InvariantCulture, $"{name}{inside}   {placement}");
            }

            builder.AppendLine();
        }

        private static void WriteInputs(StringBuilder builder, BoundFlow source)
        {
            if (source.Inputs.Count == 0)
                return;

            builder.AppendLine("Inputs:");

            foreach (FlowCsvColumn input in source.Inputs.OrderBy(x => x.OrderNumber))
            {
                // The value is never written, secret or not: data belongs in the csv beside the
                // file, and a default in the script would be the one nobody remembers to change.
                string secret = input.IsSecret ? "secret" : string.Empty;

                builder.Append("  ").AppendLine($"{Pad(Quoted(input.Name), 16)}{secret}".TrimEnd());
            }

            builder.AppendLine();
        }

        // The facts about each picture, once per file. The step says how it is searched for; this
        // says where it clicks and what size of area it was captured in, which is what lets it scale.
        private static void WriteTemplates(StringBuilder builder, BoundFlow source)
        {
            List<string> lines = source.TemplatesByStepId.Values
                .SelectMany(x => x)
                .DistinctBy(x => x.FileName, StringComparer.Ordinal)
                .OrderBy(x => x.FileName, StringComparer.Ordinal)
                .Select(TemplateLine)
                .Where(x => x.Length > 0)
                .ToList();

            if (lines.Count == 0)
                return;

            builder.AppendLine("Templates:");

            foreach (string line in lines)
                builder.Append("  ").AppendLine(line);

            builder.AppendLine();
        }

        private static string TemplateLine(ScriptTemplate template)
        {
            string click = string.Empty;
            if (template.ClickOffset != null)
                click = $"click {Integer(template.ClickOffset.Value.X)},{Integer(template.ClickOffset.Value.Y)}";

            string captured = string.Empty;
            if (template.AuthoredFlowAreaWidth > 0 && template.AuthoredFlowAreaHeight > 0)
                captured = $"captured {Integer(template.AuthoredFlowAreaWidth)}x{Integer(template.AuthoredFlowAreaHeight)}";

            string dpi = string.Empty;
            if (template.AuthoredDpi > 0)
                dpi = $"at {Dpi(template.AuthoredDpi)}";

            // "captured 800x600 at 120dpi" reads as one phrase, so it is one clause apart from the click.
            string capture = $"{captured} {dpi}".Trim();
            string facts = $"{click}   {capture}".Trim();
            if (facts.Length == 0)
                return string.Empty;

            return $"{Pad(Quoted(template.FileName), 24)}{facts}";
        }


        // ================================================================
        // Private methods - steps
        // ================================================================

        private static void WriteSteps(StringBuilder builder, BoundFlow source)
        {
            builder.AppendLine("Steps:");

            // A gap between top level steps, but never two: a marker already leaves one behind it,
            // and a heading followed by empty space reads as a section with nothing in it.
            bool needsGap = false;

            foreach (FlowStep step in source.ChildrenOf(null))
            {
                if (needsGap && step.FlowStepType != FlowStepTypeEnum.MARKER)
                    builder.AppendLine();

                WriteStep(builder, source, step, depth: 0);
                needsGap = step.FlowStepType != FlowStepTypeEnum.MARKER;
            }
        }

        private static void WriteStep(StringBuilder builder, BoundFlow source, FlowStep step, int depth)
        {
            string indent = new string(' ', depth * IndentWidth);

            // A marker is a section heading, not a step that does anything.
            if (step.FlowStepType == FlowStepTypeEnum.MARKER)
            {
                builder.AppendLine();
                builder.Append(indent).Append("## ").AppendLine(step.Name);
                builder.AppendLine();
                return;
            }

            // Intent, which is the one thing a screenshot cannot carry.
            if (!string.IsNullOrWhiteSpace(step.CodeComment))
            {
                foreach (string line in step.CodeComment.Split('\n'))
                    builder.Append(indent).Append("# ").AppendLine(line.TrimEnd('\r').Trim());
            }

            builder.Append(indent).AppendLine(StepLine(step, source));

            WriteBranches(builder, source, step, depth);

            // A container holds its steps directly - a loop's body, not a branch.
            if (TreeStepHelper.CanContainChildren(step.FlowStepType))
            {
                foreach (FlowStep child in source.ChildrenOf(step.Id))
                    WriteStep(builder, source, child, depth + 1);
            }
        }

        private static void WriteBranches(StringBuilder builder, BoundFlow source, FlowStep step, int depth)
        {
            if (!TreeStepHelper.HasBranchChildren(step.FlowStepType))
                return;

            string indent = new string(' ', (depth + 1) * IndentWidth);

            // Order comes from the rows, so two exports of one flow cannot differ. An empty branch
            // is left out entirely: "Success:" with nothing under it says nothing.
            foreach (FlowStep branch in source.ChildrenOf(step.Id))
            {
                List<FlowStep> children = source.ChildrenOf(branch.Id).ToList();
                if (children.Count == 0)
                    continue;

                string label = "Failure";
                if (branch.FlowStepType == FlowStepTypeEnum.SUCCESS)
                    label = "Success";

                builder.Append(indent).Append(label).AppendLine(":");

                foreach (FlowStep child in children)
                    WriteStep(builder, source, child, depth + 2);
            }
        }

        private static string StepLine(FlowStep step, BoundFlow source)
        {
            string keyword = Pad(SyntaxFacts.For(step), 16);
            string arguments = Arguments(step, source);

            return $"{keyword}{arguments}".TrimEnd();
        }

        private static string Arguments(FlowStep step, BoundFlow source)
        {
            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.SEARCH_IMAGE:
                    return SearchImageArguments(step, source);

                case FlowStepTypeEnum.SEARCH_TEXT:
                    return SearchTextArguments(step, source);

                case FlowStepTypeEnum.CHECK_VALUE:
                    return $"{Quoted(step.Name)}   {Variable(step.FlowStepReferenceId, source)} {SyntaxFacts.Condition(step)}";

                case FlowStepTypeEnum.CURSOR_CLICK:
                case FlowStepTypeEnum.CURSOR_RELOCATE:
                case FlowStepTypeEnum.CURSOR_DRAG:
                    return CursorArguments(step, source);

                // The area to scroll inside, not a point: Target would fall through to "match"
                // for a scroll that names neither, which is a line the parser cannot read back.
                case FlowStepTypeEnum.CURSOR_SCROLL:
                    return $"{SyntaxFacts.ScrollDirection(step.CursorScrollDirectionType)} {step.LoopCount}{Area(step, source)}";

                case FlowStepTypeEnum.KEYBOARD_INPUT:
                    if (step.KeyboardInputType == KeyboardInputTypeEnum.COMBINATION)
                        return step.KeyboardInputText;

                    return Quoted(step.KeyboardInputText);

                case FlowStepTypeEnum.WAIT:
                    if (step.WaitForMillisecondsMax > step.WaitForMilliseconds)
                        return $"{step.WaitForMilliseconds}ms to {step.WaitForMillisecondsMax}ms";

                    return $"{step.WaitForMilliseconds}ms";

                case FlowStepTypeEnum.LOOP:
                    return LoopArguments(step, source);

                case FlowStepTypeEnum.GO_TO:
                    return $"to {Quoted(source.StepNamesById.GetValueOrDefault(step.FlowStepReferenceId ?? 0, string.Empty))}";

                case FlowStepTypeEnum.SUB_FLOW:
                    return Quoted(source.SubFlowPathsById.GetValueOrDefault(step.SubFlowId ?? 0, string.Empty));

                case FlowStepTypeEnum.NOTIFY:
                    return Quoted(step.Message);

                case FlowStepTypeEnum.END_EXECUTION:
                    if (step.EndExecutionAsSuccess)
                        return "passed";

                    return $"failed  {Quoted(step.Message)}";

                case FlowStepTypeEnum.SYSTEM_ACTION:
                    return step.SystemActionType.ToString();

                case FlowStepTypeEnum.SYSTEM_COMMAND:
                    return CommandArguments(step);

                case FlowStepTypeEnum.WINDOW_FOCUS:
                case FlowStepTypeEnum.WINDOW_RESIZE:
                case FlowStepTypeEnum.WINDOW_RELOCATE:
                    return WindowArguments(step, source);

                default:
                    return Quoted(step.Name);
            }
        }

        private static string SearchImageArguments(FlowStep step, BoundFlow source)
        {
            IEnumerable<string> templates = source.TemplatesByStepId
                .GetValueOrDefault(step.Id, [])
                .Select(TemplateClause);

            // Only when it is not the default, which is nearly every step.
            string match = string.Empty;
            if (step.TemplateMatchMode != TemplateMatchModeEnum.SHAPE)
                match = $"   match {SyntaxFacts.MatchMode(step.TemplateMatchMode)}";

            return $"{Quoted(step.Name)}   {string.Join("  ", templates)}{match}{Area(step, source)}{Waiting(step)}";
        }

        // Straight after its template, so it reads as that template's and not the step's.
        private static string TemplateClause(ScriptTemplate template)
        {
            string required = string.Empty;
            if (template.IsRequired)
                required = " required";

            return $"template {Quoted(template.FileName)} accuracy {Number(template.Accuracy)}{required}";
        }

        private static string SearchTextArguments(FlowStep step, BoundFlow source)
        {
            string extract = string.Empty;
            if (!string.IsNullOrWhiteSpace(step.ResultExtractPattern))
                extract = $"   keep {Quoted(step.ResultExtractPattern)}";

            return $"{Quoted(step.Name)}   {SyntaxFacts.Condition(step)}{Area(step, source)}{extract}{Waiting(step)}";
        }

        private static string CursorArguments(FlowStep step, BoundFlow source)
        {
            string target = Target(step, source, "at ");

            if (step.FlowStepType == FlowStepTypeEnum.CURSOR_RELOCATE)
                return $"to {target.TrimStart()}".Replace("to at ", "to ");

            if (step.FlowStepType == FlowStepTypeEnum.CURSOR_DRAG)
                return $"{target.TrimStart()} to {PointName(step.FlowPointEndId, step.FlowStepReferenceEndId, source)}";

            string button = SyntaxFacts.Button(step.CursorButtonType, step.CursorButtonActionType);
            if (button.Length == 0)
                return target.TrimStart();

            return $"{target.TrimStart()}   {button}";
        }

        private static string LoopArguments(FlowStep step, BoundFlow source)
        {
            // Three sources, and which one is in play is readable from the row rather than stored:
            // a reference means each match, no count means forever.
            if (step.FlowStepReferenceId != null)
                return $"each match in {Quoted(source.StepNamesById.GetValueOrDefault(step.FlowStepReferenceId.Value, string.Empty))}";

            return step.IsLoopInfinite ? "forever" : $"{step.LoopCount} times";
        }

        private static string CommandArguments(FlowStep step)
        {
            if (step.RunCommandPreset == RunCommandPresetEnum.LAUNCH_APP)
                return Quoted(step.RunCommandValue);

            string preset = string.Empty;
            if (step.RunCommandPreset != RunCommandPresetEnum.CUSTOM)
                preset = $"{step.RunCommandPreset} ";

            return $"{preset}{Quoted(step.RunCommandValue)}";
        }

        private static string WindowArguments(FlowStep step, BoundFlow source)
        {
            string title = string.Empty;
            if (!string.IsNullOrWhiteSpace(step.TitlePattern))
                title = $" title {SyntaxFacts.TitleMatch(step.TitleMatchMode)} {Quoted(step.TitlePattern)}";

            string what = $"process {Quoted(step.ProcessName)}{title}";

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RESIZE)
                return $"{what}   size {step.WindowWidth} {step.WindowHeight}";

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RELOCATE)
                return $"{what}   to {PointName(step.FlowPointId, null, source)}";

            return what;
        }


        // ================================================================
        // Private methods - fragments
        // ================================================================

        private static string Area(FlowStep step, BoundFlow source)
        {
            if (step.FlowAreaId == null)
                return string.Empty;

            return $"   in {Quoted(source.AreaNamesById.GetValueOrDefault(step.FlowAreaId.Value, string.Empty))}";
        }

        private static string Waiting(FlowStep step)
        {
            bool isWaiting = step.SearchMode == SearchModeEnum.WAIT_UNTIL_FOUND
                             || step.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND;

            if (!isWaiting)
                return string.Empty;

            // Zero is "for ever", and writing "timeout 0s" would read as "give up at once".
            if (step.TimeoutMilliseconds > 0)
                return $"   timeout {Seconds(step.TimeoutMilliseconds)}";

            return "   no timeout";
        }

        private static string Target(FlowStep step, BoundFlow source, string lead)
        {
            return $"{lead}{PointName(step.FlowPointId, step.FlowStepReferenceId, source)}";
        }

        private static string PointName(int? flowPointId, int? referenceId, BoundFlow source)
        {
            if (flowPointId != null)
                return $"point {Quoted(source.PointNamesById.GetValueOrDefault(flowPointId.Value, string.Empty))}";

            if (referenceId != null)
                return Quoted(source.StepNamesById.GetValueOrDefault(referenceId.Value, string.Empty));

            return "match";
        }

        private static string Variable(int? referenceId, BoundFlow source)
        {
            if (referenceId == null)
                return "\"\"";

            return Quoted("{{" + source.StepNamesById.GetValueOrDefault(referenceId.Value, string.Empty) + "}}");
        }

        private static IEnumerable<FlowArea> Ordered(IReadOnlyList<FlowArea> areas)
        {
            List<FlowArea> roots = areas
                .Where(x => x.ParentFlowAreaId == null)
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToList();

            foreach (FlowArea root in roots)
            {
                yield return root;

                foreach (FlowArea child in areas.Where(x => x.ParentFlowAreaId == root.Id).OrderBy(x => x.Name, StringComparer.Ordinal))
                    yield return child;
            }
        }

        private static string Seconds(int milliseconds)
        {
            if (milliseconds % 1000 == 0)
                return $"{milliseconds / 1000}s";

            return $"{Number(milliseconds / 1000f)}s";
        }

        private static string Dpi(int dpi)
        {
            return $"{Integer(dpi)}dpi";
        }

        private static string Ratio(float value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string Number(float value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        // A machine reads this file back. Only the negative sign varies between cultures for an
        // integer, but "offset −5 10" is still a line the parser cannot take.
        private static string Integer(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Pad(string text, int width)
        {
            return text.Length >= width ? text + "  " : text.PadRight(width);
        }

        // Always quoted, even when a name would read fine without. One rule is easier to parse back
        // than a rule about which names need it.
        private static string Quoted(string? text)
        {
            return "\"" + (text ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }
    }
}
