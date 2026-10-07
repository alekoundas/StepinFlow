using System.Globalization;
using System.Text;

using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Business.FlowScript.Catalogs;
using Business.FlowScript.Models;
using Business.FlowScript.Syntax;

namespace Business.FlowScript.Text
{
    /// <summary>
    /// A flow as the text that goes in a repository. The mirror of Scanner.cs over the same model,
    /// so a name is written through its link (<c>step.FlowArea.Name</c>).
    ///
    /// See FLOW-FORMAT.md
    ///
    /// Deterministic.
    /// </summary>
    public sealed class Printer : IPrinter
    {
        public string Write(FlowScriptSchema schema)
        {
            StringBuilder builder = new StringBuilder();

            WriteHeader(builder, schema);
            WriteSteps(builder, schema);

            return builder.ToString();
        }


        // ================================================================
        // Private methods - header
        // ================================================================

        private static void WriteHeader(StringBuilder builder, FlowScriptSchema schema)
        {
            builder.Append(Pad(ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.FLOWFIELD_NAME), 9)).AppendLine(schema.Flow.Name);
            builder.Append(Pad(ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.FLOWFIELD_ID), 9)).AppendLine(schema.Flow.PublicId.ToString());

            if (schema.Viewports.Count > 0)
            {
                IEnumerable<string> sizes = schema.Viewports
                    .OrderBy(x => x.OrderNumber)
                    .Select(x => Size(x.Width, x.Height));

                builder.Append(Pad(ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.FLOWFIELD_SIZES), 9)).AppendLine(string.Join(" ", sizes));
            }

            builder.AppendLine();

            WriteAreas(builder, schema);
            WritePoints(builder, schema);
            WriteInputs(builder, schema);
            WriteTemplates(builder, schema);
        }

        private static void WriteAreas(StringBuilder builder, FlowScriptSchema schema)
        {
            if (schema.Areas.Count == 0)
                return;

            builder.AppendLine(ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.AREAS));

            // Parents before children, so a child's "inside X" always names something already read.
            foreach (FlowArea area in Ordered(schema.Areas))
                builder.Append("  ").AppendLine(AreaLine(area));

            builder.AppendLine();
        }

        private static string AreaLine(FlowArea area)
        {
            string name = Pad(SyntaxFacts.Quote(area.Name), 20);

            return $"{name}{AreaPlacement(area)}{AreaScaling(area)}";
        }

        private static string AreaPlacement(FlowArea area)
        {
            if (area.ParentFlowArea != null)
                return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.INSIDE)} {SyntaxFacts.Quote(area.ParentFlowArea.Name)}   {AreaSize(area)}";

            switch (area.Type)
            {
                // Empty is the primary monitor, and "primary" is a bare word so a device cannot be mistaken for it.
                case FlowAreaTypeEnum.MONITOR:
                    if (area.MonitorDeviceName.Length == 0)
                        return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.MONITOR)} {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.PRIMARY)}";

                    return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.MONITOR)} {SyntaxFacts.Quote(area.MonitorDeviceName)}";

                case FlowAreaTypeEnum.CUSTOM:
                    return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.ON_SCREEN)}   {AreaSize(area)}";

                default:
                    return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.WINDOW)} {Process(area.ProcessName, area.TitleMatchMode, area.TitlePattern)}";
            }
        }

        private static string AreaSize(FlowArea area)
        {
            if (area.SizingMode == AreaSizingModeEnum.RATIO)
                return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.RATIO)} {Ratio(area.RatioX)} {Ratio(area.RatioY)}  {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.SIZE)} {Ratio(area.RatioWidth)} {Ratio(area.RatioHeight)}";

            return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.OFFSET)} {Integer(area.LocationX)} {Integer(area.LocationY)}  {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.SIZE)} {Integer(area.Width)} {Integer(area.Height)}";
        }

        // Left out when unset: no setting inherits the parent's, and no DPI leaves pixels as they are.
        private static string AreaScaling(FlowArea area)
        {
            string text = string.Empty;

            if (area.ScalesWith != null)
                text += $"   {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.SCALES_WITH)} {ScriptKeywordCatalog.GetTextOfKeyword(area.ScalesWith.Value)}";

            if (area.AuthoredDpi > 0)
                text += $"   {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.AT)} {Dpi(area.AuthoredDpi)}";

            return text;
        }

        private static void WritePoints(StringBuilder builder, FlowScriptSchema schema)
        {
            if (schema.Points.Count == 0)
                return;

            builder.AppendLine(ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.POINTS));

            foreach (FlowPoint point in schema.Points.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                string name = Pad(SyntaxFacts.Quote(point.Name), 20);

                string placement = $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.OFFSET)} {Integer(point.LocationX)} {Integer(point.LocationY)}";
                if (point.OffsetMode == AreaSizingModeEnum.RATIO)
                    placement = $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.RATIO)} {Ratio(point.RatioX)} {Ratio(point.RatioY)}";

                string inside = ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.ON_SCREEN);
                if (point.FlowArea != null)
                    inside = $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.INSIDE)} {SyntaxFacts.Quote(point.FlowArea.Name)}";

                string dpi = string.Empty;
                if (point.AuthoredDpi > 0)
                    dpi = $"   {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.AT)} {Dpi(point.AuthoredDpi)}";

                builder.Append("  ").AppendLine(CultureInfo.InvariantCulture, $"{name}{inside}   {placement}{dpi}");
            }

            builder.AppendLine();
        }

        private static void WriteInputs(StringBuilder builder, FlowScriptSchema schema)
        {
            if (schema.Inputs.Count == 0)
                return;

            builder.AppendLine(ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.CSV_COLUMNS));

            foreach (FlowCsvColumn input in schema.Inputs.OrderBy(x => x.OrderNumber))
            {
                // The value is never written, secret or not: data belongs in the csv beside the
                // file, and a default in the script would be the one nobody remembers to change.
                string secret = string.Empty;
                if (input.IsSecret)
                    secret = ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.SECRET);

                builder.Append("  ").AppendLine($"{Pad(SyntaxFacts.Quote(input.Name), 20)}{secret}".TrimEnd());
            }

            builder.AppendLine();
        }

        // The facts about each picture, once per file. The step says how it is searched for; this
        // says where it clicks and what size of area it was captured in, which is what lets it scale.
        private static void WriteTemplates(StringBuilder builder, FlowScriptSchema schema)
        {
            List<string> lines = schema.Steps
                .SelectMany(x => x.FlowStepTemplates)
                .DistinctBy(x => x.Name, StringComparer.Ordinal)
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .Select(TemplateLine)
                .ToList();

            if (lines.Count == 0)
                return;

            builder.AppendLine(ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.TEMPLATES));

            foreach (string line in lines)
                builder.Append("  ").AppendLine(line);

            builder.AppendLine();
        }

        // A template is named by its file, and every one says where it is clicked.
        private static string TemplateLine(FlowStepTemplate template)
        {
            string click = $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.CLICK)} {Integer(template.ClickOffsetX)} {Integer(template.ClickOffsetY)}";

            string captured = string.Empty;
            if (template.AuthoredFlowAreaWidth > 0 && template.AuthoredFlowAreaHeight > 0)
                captured = $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.CAPTURED)} {Size(template.AuthoredFlowAreaWidth, template.AuthoredFlowAreaHeight)}";

            string dpi = string.Empty;
            if (template.AuthoredDpi > 0)
                dpi = $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.AT)} {Dpi(template.AuthoredDpi)}";

            // "captured 800x600 at 120dpi" reads as one phrase, so it is one clause apart from the click.
            string capture = $"{captured} {dpi}".Trim();
            string facts = $"{click}   {capture}".Trim();

            return $"{Pad(SyntaxFacts.Quote(template.Name), 28)}{facts}";
        }


        // ================================================================
        // Private methods - steps
        // ================================================================

        private static void WriteSteps(StringBuilder builder, FlowScriptSchema schema)
        {
            builder.AppendLine(ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.STEPS));

            // A gap between top level steps, but never two: a marker already leaves one behind it,
            // and a heading followed by empty space reads as a section with nothing in it.
            bool needsGap = false;

            foreach (FlowStep step in ChildrenOf(schema, null))
            {
                if (needsGap && step.FlowStepType != FlowStepTypeEnum.STAGE_MARKER)
                    builder.AppendLine();

                WriteStep(builder, schema, step, depth: 0);
                needsGap = step.FlowStepType != FlowStepTypeEnum.STAGE_MARKER;
            }
        }

        private static void WriteStep(StringBuilder builder, FlowScriptSchema schema, FlowStep step, int depth)
        {
            string indent = new string(' ', depth);

            // A marker is a section heading, not a step that does anything.
            if (step.FlowStepType == FlowStepTypeEnum.STAGE_MARKER)
            {
                builder.AppendLine();
                WriteComment(builder, step, indent);
                builder.Append(indent).Append(SyntaxFacts.For(step)).Append(' ').AppendLine(step.Name);
                builder.AppendLine();
                return;
            }

            WriteComment(builder, step, indent);
            builder.Append(indent).AppendLine(StepLine(step, schema));

            WriteBranches(builder, schema, step, depth);

            // A container holds its steps directly - a loop's body, not a branch.
            if (TreeStepHelper.CanContainChildren(step.FlowStepType))
            {
                foreach (FlowStep child in ChildrenOf(schema, step))
                    WriteStep(builder, schema, child, depth + 1);
            }
        }

        private static void WriteBranches(StringBuilder builder, FlowScriptSchema schema, FlowStep step, int depth)
        {
            if (!TreeStepHelper.HasBranchChildren(step.FlowStepType))
                return;

            string indent = new string(' ', depth + 1);

            // Order comes from the rows, so two exports of one flow cannot differ. An empty branch
            // is left out entirely - "Success:" with nothing under it says nothing - unless its comment does.
            foreach (FlowStep branch in ChildrenOf(schema, step))
            {
                List<FlowStep> children = ChildrenOf(schema, branch).ToList();
                if (children.Count == 0 && string.IsNullOrWhiteSpace(branch.CodeComment))
                    continue;

                WriteComment(builder, branch, indent);
                builder.Append(indent).AppendLine(SyntaxFacts.For(branch));

                foreach (FlowStep child in children)
                    WriteStep(builder, schema, child, depth + 2);
            }
        }

        // Intent, which is the one thing a screenshot cannot carry, above the line it is for.
        private static void WriteComment(StringBuilder builder, FlowStep step, string indent)
        {
            if (string.IsNullOrWhiteSpace(step.CodeComment))
                return;

            foreach (string line in step.CodeComment.Split('\n'))
                builder.Append(indent).Append(ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.COMMENT)).Append(' ').AppendLine(line.TrimEnd('\r').Trim());
        }

        private static string StepLine(FlowStep step, FlowScriptSchema schema)
        {
            string keyword = Pad(SyntaxFacts.For(step), 16);
            string arguments = Arguments(step, schema);

            return $"{keyword}{arguments}".TrimEnd();
        }

        private static string Arguments(FlowStep step, FlowScriptSchema schema)
        {
            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.SEARCH_IMAGE:
                    return SearchImageArguments(step);

                case FlowStepTypeEnum.SEARCH_TEXT:
                    return SearchTextArguments(step);

                case FlowStepTypeEnum.CHECK_VALUE:
                    return $"{SyntaxFacts.Quote(step.Name)}   {Variable(step.FlowStepReference)} {SyntaxFacts.Condition(step)}";

                case FlowStepTypeEnum.CURSOR_CLICK:
                case FlowStepTypeEnum.CURSOR_RELOCATE:
                case FlowStepTypeEnum.CURSOR_DRAG:
                    return CursorArguments(step);

                // The area to scroll inside, not a point: Target would fall through to "match"
                // for a scroll that names neither, which is a line the parser cannot read back.
                // No direction goes down, which is what a scroll did before there was one.
                case FlowStepTypeEnum.CURSOR_SCROLL:
                    return $"{ScriptKeywordCatalog.GetTextOfKeyword(step.CursorScrollDirectionType ?? CursorScrollDirectionTypeEnum.DOWN)} {step.LoopCount}{Area(step)}";

                case FlowStepTypeEnum.KEYBOARD_INPUT:
                    return SyntaxFacts.Quote(step.KeyboardInputText);

                case FlowStepTypeEnum.WAIT:
                    if (step.WaitForMillisecondsMax > step.WaitForMilliseconds)
                        return $"{Milliseconds(step.WaitForMilliseconds)} {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.TO)} {Milliseconds(step.WaitForMillisecondsMax)}";

                    return Milliseconds(step.WaitForMilliseconds);

                case FlowStepTypeEnum.LOOP:
                    return LoopArguments(step);

                case FlowStepTypeEnum.GO_BACK:
                    return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.TO)} {SyntaxFacts.Quote(step.FlowStepReference?.Name ?? string.Empty)}";

                case FlowStepTypeEnum.SUB_FLOW:
                    return SyntaxFacts.Quote(schema.SubFlowPaths.GetValueOrDefault(step, string.Empty));

                case FlowStepTypeEnum.NOTIFY:
                    return SyntaxFacts.Quote(step.Message);

                case FlowStepTypeEnum.END_EXECUTION:
                    if (step.EndExecutionAsSuccess)
                        return ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.PASSED);

                    return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.FAILED)}  {SyntaxFacts.Quote(step.Message)}";

                case FlowStepTypeEnum.SYSTEM_ACTION:
                    return ScriptKeywordCatalog.GetTextOfKeyword(step.SystemActionType);

                case FlowStepTypeEnum.SYSTEM_COMMAND:
                    return CommandArguments(step);

                case FlowStepTypeEnum.WINDOW_FOCUS:
                case FlowStepTypeEnum.WINDOW_RESIZE:
                case FlowStepTypeEnum.WINDOW_RELOCATE:
                    return WindowArguments(step);

                default:
                    return SyntaxFacts.Quote(step.Name);
            }
        }

        private static string SearchImageArguments(FlowStep step)
        {
            IEnumerable<string> templates = step.FlowStepTemplates
                .OrderBy(x => x.OrderNumber)
                .Select(TemplateClause);

            // Only when it is not the default, which is nearly every step.
            string match = string.Empty;
            if (step.TemplateMatchMode != TemplateMatchModeEnum.SHAPE)
                match = $"   {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.MATCH)} {ScriptKeywordCatalog.GetTextOfKeyword(step.TemplateMatchMode)}";

            return $"{SyntaxFacts.Quote(step.Name)}   {string.Join("  ", templates)}{match}{Area(step)}{Waiting(step)}";
        }

        // Straight after its template, so it reads as that template's and not the step's.
        private static string TemplateClause(FlowStepTemplate template)
        {
            string required = string.Empty;
            if (template.IsRequired)
                required = $" {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.REQUIRED)}";

            return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.TEMPLATE)} {SyntaxFacts.Quote(template.Name)} {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.ACCURACY)} {Number(template.Accuracy)}{required}";
        }

        private static string SearchTextArguments(FlowStep step)
        {
            string extract = string.Empty;
            if (!string.IsNullOrWhiteSpace(step.ResultExtractPattern))
                extract = $"   {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.KEEP)} {SyntaxFacts.Quote(step.ResultExtractPattern)}";

            return $"{SyntaxFacts.Quote(step.Name)}   {SyntaxFacts.Condition(step)}{Area(step)}{extract}{Waiting(step)}";
        }

        private static string CursorArguments(FlowStep step)
        {
            string target = Target(step.FlowPoint, step.FlowStepReference);

            if (step.FlowStepType == FlowStepTypeEnum.CURSOR_RELOCATE)
                return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.TO)} {target}";

            string at = $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.AT)} {target}";

            if (step.FlowStepType == FlowStepTypeEnum.CURSOR_DRAG)
                return $"{at} {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.TO)} {Target(step.FlowPointEnd, step.FlowStepReferenceEnd)}";

            string button = SyntaxFacts.Button(step.CursorButtonType, step.CursorButtonActionType);
            if (button.Length == 0)
                return at;

            return $"{at}   {button}";
        }

        private static string LoopArguments(FlowStep step)
        {
            // Three sources, and which one is in play is readable from the row rather than stored:
            // a reference means each match, no count means forever.
            if (step.FlowStepReference != null)
            {
                string each = $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.EACH)} {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.MATCH)} {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.IN)}";
                return $"{each} {SyntaxFacts.Quote(step.FlowStepReference.Name)}";
            }

            if (step.IsLoopInfinite)
                return ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.FOREVER);

            return $"{step.LoopCount} {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.TIMES)}";
        }

        private static string CommandArguments(FlowStep step)
        {
            if (step.RunCommandPreset == RunCommandPresetEnum.LAUNCH_APP)
                return SyntaxFacts.Quote(step.RunCommandValue);

            string preset = string.Empty;
            if (step.RunCommandPreset != RunCommandPresetEnum.CUSTOM)
                preset = $"{ScriptKeywordCatalog.GetTextOfKeyword(step.RunCommandPreset)} ";

            return $"{preset}{SyntaxFacts.Quote(step.RunCommandValue)}";
        }

        private static string WindowArguments(FlowStep step)
        {
            string what = Process(step.ProcessName, step.TitleMatchMode, step.TitlePattern);

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RESIZE)
                return $"{what}   {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.SIZE)} {step.WindowWidth} {step.WindowHeight}";

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RELOCATE)
                return $"{what}   {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.TO)} {Target(step.FlowPoint, null)}";

            return what;
        }


        // ================================================================
        // Private methods - fragments
        // ================================================================

        // A step's children in running order: a loop's body, or what sits under a branch.
        private static IEnumerable<FlowStep> ChildrenOf(FlowScriptSchema schema, FlowStep? parent)
        {
            return schema.Steps
                .Where(x => x.ParentFlowStep == parent)
                .OrderBy(x => x.OrderNumber);
        }

        private static string Area(FlowStep step)
        {
            if (step.FlowArea == null)
                return string.Empty;

            return $"   {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.IN)} {SyntaxFacts.Quote(step.FlowArea.Name)}";
        }

        // The window an area or a step is about: the process, and optionally its title.
        private static string Process(string processName, TitleMatchModeEnum titleMatchMode, string titlePattern)
        {
            string title = string.Empty;
            if (!string.IsNullOrWhiteSpace(titlePattern))
                title = $" {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.TITLE)} {ScriptKeywordCatalog.GetTextOfKeyword(titleMatchMode)} {SyntaxFacts.Quote(titlePattern)}";

            return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.PROCESS)} {SyntaxFacts.Quote(processName)}{title}";
        }

        private static string Waiting(FlowStep step)
        {
            bool isWaiting = step.SearchMode == SearchModeEnum.WAIT_UNTIL_FOUND
                             || step.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND;

            if (!isWaiting)
                return string.Empty;

            // Zero is "for ever", and writing "timeout 0s" would read as "give up at once".
            if (step.TimeoutMilliseconds > 0)
                return $"   {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.TIMEOUT)} {Milliseconds(step.TimeoutMilliseconds)}";

            return $"   {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.NO_TIMEOUT)}";
        }

        private static string Target(FlowPoint? point, FlowStep? reference)
        {
            if (point != null)
                return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.POINT)} {SyntaxFacts.Quote(point.Name)}";

            if (reference != null)
                return SyntaxFacts.Quote(reference.Name);

            return ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.MATCH);
        }

        private static string Variable(FlowStep? reference)
        {
            if (reference == null)
                return SyntaxFacts.Quote(string.Empty);

            return SyntaxFacts.Quote("{{" + reference.Name + "}}");
        }

        private static IEnumerable<FlowArea> Ordered(IReadOnlyList<FlowArea> areas)
        {
            List<FlowArea> roots = areas
                .Where(x => x.ParentFlowArea == null)
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToList();

            foreach (FlowArea root in roots)
            {
                yield return root;

                foreach (FlowArea child in areas.Where(x => x.ParentFlowArea == root).OrderBy(x => x.Name, StringComparer.Ordinal))
                    yield return child;
            }
        }

        private static string Milliseconds(int milliseconds)
        {
            return $"{Integer(milliseconds)}{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.MILLISECONDS)}";
        }

        private static string Dpi(int dpi)
        {
            return $"{Integer(dpi)}{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.DPI)}";
        }

        private static string Size(int width, int height)
        {
            return $"{Integer(width)}{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.SIZE_SEPARATOR)}{Integer(height)}";
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

        // Add text and the remaining width turned to spaces
        private static string Pad(string text, int width)
        {
            if (text.Length >= width)
                return text + "  ";
            else
                return text.PadRight(width);
        }
    }
}
