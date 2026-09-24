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

            if (area.ParentFlowAreaId == null)
            {
                // A root area binds to a window. Nothing else can be placed inside anything.
                string title = string.IsNullOrWhiteSpace(area.TitlePattern)
                    ? string.Empty
                    : $" title {SyntaxFacts.TitleMatch(area.TitleMatchMode)} {Quoted(area.TitlePattern)}";

                return $"{name}window process {Quoted(area.ProcessName)}{title}";
            }

            string parent = Quoted(source.AreaNamesById.GetValueOrDefault(area.ParentFlowAreaId.Value, string.Empty));
            string placement = area.SizingMode == AreaSizingModeEnum.RATIO
                ? $"ratio {Ratio(area.RatioX)} {Ratio(area.RatioY)}  {Ratio(area.RatioWidth)} {Ratio(area.RatioHeight)}"
                : $"offset {area.LocationX} {area.LocationY}  size {area.Width} {area.Height}";

            return $"{name}inside {parent}   {placement}";
        }

        private static void WritePoints(StringBuilder builder, BoundFlow source)
        {
            if (source.Points.Count == 0)
                return;

            builder.AppendLine("Points:");

            foreach (FlowPoint point in source.Points.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                string name = Pad(Quoted(point.Name), 16);
                string placement = point.OffsetMode == AreaSizingModeEnum.RATIO
                    ? $"ratio {Ratio(point.RatioX)} {Ratio(point.RatioY)}"
                    : $"offset {Integer(point.LocationX)} {Integer(point.LocationY)}";

                string inside = point.FlowAreaId == null
                    ? "on screen"
                    : $"inside {Quoted(source.AreaNamesById.GetValueOrDefault(point.FlowAreaId.Value, string.Empty))}";

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

                builder.Append(indent).Append(branch.FlowStepType == FlowStepTypeEnum.SUCCESS ? "Success" : "Failure").AppendLine(":");

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
                    return step.KeyboardInputType == KeyboardInputTypeEnum.COMBINATION
                        ? step.KeyboardInputText
                        : Quoted(step.KeyboardInputText);

                case FlowStepTypeEnum.WAIT:
                    return step.WaitForMillisecondsMax > step.WaitForMilliseconds
                        ? $"{step.WaitForMilliseconds}ms to {step.WaitForMillisecondsMax}ms"
                        : $"{step.WaitForMilliseconds}ms";

                case FlowStepTypeEnum.LOOP:
                    return LoopArguments(step, source);

                case FlowStepTypeEnum.GO_TO:
                    return $"to {Quoted(source.StepNamesById.GetValueOrDefault(step.FlowStepReferenceId ?? 0, string.Empty))}";

                case FlowStepTypeEnum.SUB_FLOW:
                    return Quoted(source.SubFlowPathsById.GetValueOrDefault(step.SubFlowId ?? 0, string.Empty));

                case FlowStepTypeEnum.NOTIFY:
                    return Quoted(step.Message);

                case FlowStepTypeEnum.END_EXECUTION:
                    return step.EndExecutionAsSuccess
                        ? "passed"
                        : $"failed  {Quoted(step.Message)}";

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
            // Straight after its template, so it reads as that template's and not the step's.
            IEnumerable<string> templates = source.TemplatesByStepId
                .GetValueOrDefault(step.Id, [])
                .Select(x => $"template {Quoted(x.FileName)} accuracy {Number(x.Accuracy)}");

            return $"{Quoted(step.Name)}   {string.Join("  ", templates)}{Area(step, source)}{Waiting(step)}";
        }

        private static string SearchTextArguments(FlowStep step, BoundFlow source)
        {
            string extract = string.IsNullOrWhiteSpace(step.ResultExtractPattern)
                ? string.Empty
                : $"   keep {Quoted(step.ResultExtractPattern)}";

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

            return $"{target.TrimStart()}{(button.Length == 0 ? string.Empty : "   " + button)}";
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

            string preset = step.RunCommandPreset == RunCommandPresetEnum.CUSTOM
                ? string.Empty
                : $"{step.RunCommandPreset} ";

            return $"{preset}{Quoted(step.RunCommandValue)}";
        }

        private static string WindowArguments(FlowStep step, BoundFlow source)
        {
            string title = string.IsNullOrWhiteSpace(step.TitlePattern)
                ? string.Empty
                : $" title {SyntaxFacts.TitleMatch(step.TitleMatchMode)} {Quoted(step.TitlePattern)}";

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
            return step.TimeoutMilliseconds > 0
                ? $"   timeout {Seconds(step.TimeoutMilliseconds)}"
                : "   no timeout";
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
            return milliseconds % 1000 == 0
                ? $"{milliseconds / 1000}s"
                : $"{Number(milliseconds / 1000f)}s";
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
