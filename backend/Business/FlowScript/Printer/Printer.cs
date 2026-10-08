using System.Text;

using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Business.FlowScript.Catalogs;
using Business.FlowScript.Models;
using Business.FlowScript.Text.Header;
using Business.FlowScript.Text.Steps;
using Business.FlowScript.Text.Structure;

namespace Business.FlowScript.Text
{
    /// <summary>
    /// A flow as the text that goes in a repository. The mirror of Scanner.cs over the same model:
    /// this decides which line goes where - the sections, the order, the indentation - and a
    /// writer per kind of line writes each one, as a parser per kind of line reads it.
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

        // The writer for a step's line, by its type - the mirror of ScriptLineParser choosing a
        // parser. A type with none throws rather than writing a line no reader accepts.
        internal static BaseStepWriter StepWriter(FlowStep step, FlowScriptSchema schema)
        {
            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.SEARCH_IMAGE:
                    return new SearchImageWriter(step);

                case FlowStepTypeEnum.SEARCH_TEXT:
                    return new SearchTextWriter(step);

                case FlowStepTypeEnum.CHECK_VALUE:
                    return new CheckValueWriter(step);

                case FlowStepTypeEnum.CURSOR_CLICK:
                    return new CursorClickWriter(step);

                case FlowStepTypeEnum.CURSOR_RELOCATE:
                    return new CursorRelocateWriter(step);

                case FlowStepTypeEnum.CURSOR_DRAG:
                    return new CursorDragWriter(step);

                case FlowStepTypeEnum.CURSOR_SCROLL:
                    return new CursorScrollWriter(step);

                case FlowStepTypeEnum.KEYBOARD_INPUT:
                    return new KeyboardInputWriter(step);

                case FlowStepTypeEnum.WAIT:
                    return new WaitWriter(step);

                case FlowStepTypeEnum.LOOP:
                    return new LoopWriter(step);

                case FlowStepTypeEnum.GO_BACK:
                    return new GoBackWriter(step);

                case FlowStepTypeEnum.SUB_FLOW:
                    return new SubFlowWriter(step, schema.SubFlowPaths.GetValueOrDefault(step, string.Empty));

                case FlowStepTypeEnum.NOTIFY:
                    return new NotifyWriter(step);

                case FlowStepTypeEnum.END_EXECUTION:
                    return new EndExecutionWriter(step);

                case FlowStepTypeEnum.SYSTEM_ACTION:
                    return new SystemActionWriter(step);

                case FlowStepTypeEnum.SYSTEM_COMMAND:
                    return new SystemCommandWriter(step);

                case FlowStepTypeEnum.WINDOW_FOCUS:
                    return new WindowFocusWriter(step);

                case FlowStepTypeEnum.WINDOW_RESIZE:
                    return new WindowResizeWriter(step);

                case FlowStepTypeEnum.WINDOW_RELOCATE:
                    return new WindowRelocateWriter(step);

                case FlowStepTypeEnum.STAGE_MARKER:
                    return new StageMarkerWriter(step);

                case FlowStepTypeEnum.SUCCESS:
                case FlowStepTypeEnum.FAILURE:
                    return new BranchWriter(step);

                default:
                    throw new InvalidOperationException($"No writer writes a {step.FlowStepType} step.");
            }
        }


        // ================================================================
        // Private methods - header
        // ================================================================

        private static void WriteHeader(StringBuilder builder, FlowScriptSchema schema)
        {
            builder.AppendLine(new FlowNameWriter(schema.Flow).Write());
            builder.AppendLine(new FlowIdWriter(schema.Flow).Write());
            builder.AppendLine(new FlowSizesWriter(schema.Viewports).Write());

            builder.AppendLine();

            // Parents before children, so a child's "inside X" always names something already read.
            WriteSection(builder, ScriptSymbolEnum.AREAS, Ordered(schema.Areas).Select(x => new AreaWriter(x)));
            WriteSection(builder, ScriptSymbolEnum.POINTS, schema.Points.OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => new PointWriter(x)));
            WriteSection(builder, ScriptSymbolEnum.CSV_COLUMNS, schema.Inputs.OrderBy(x => x.OrderNumber).Select(x => new CsvColumnWriter(x)));

            // The facts about each picture, once per file. The step says how it is searched for; this
            // says where it clicks and what size of area it was captured in, which is what lets it scale.
            IEnumerable<FlowStepTemplate> templates = schema.Steps
                .SelectMany(x => x.FlowStepTemplates)
                .DistinctBy(x => x.Name, StringComparer.Ordinal)
                .OrderBy(x => x.Name, StringComparer.Ordinal);

            WriteSection(builder, ScriptSymbolEnum.TEMPLATES, templates.Select(x => new TemplateWriter(x)));
        }

        // The heading and its lines indented under it, or nothing for a section with no lines.
        private static void WriteSection(StringBuilder builder, ScriptSymbolEnum section, IEnumerable<BaseWriter> writers)
        {
            List<string> lines = writers.Select(x => x.Write()).ToList();
            if (lines.Count == 0)
                return;

            builder.AppendLine(new SectionWriter(section).Write());

            foreach (string line in lines)
                builder.Append("  ").AppendLine(line);

            builder.AppendLine();
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


        // ================================================================
        // Private methods - steps
        // ================================================================

        private static void WriteSteps(StringBuilder builder, FlowScriptSchema schema)
        {
            builder.AppendLine(new SectionWriter(ScriptSymbolEnum.STEPS).Write());

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
                builder.Append(indent).AppendLine(StepWriter(step, schema).Write());
                builder.AppendLine();
                return;
            }

            WriteComment(builder, step, indent);
            builder.Append(indent).AppendLine(StepWriter(step, schema).Write());

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
                builder.Append(indent).AppendLine(StepWriter(branch, schema).Write());

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
                builder.Append(indent).AppendLine(new CommentWriter(line).Write());
        }

        // A step's children in running order: a loop's body, or what sits under a branch.
        private static IEnumerable<FlowStep> ChildrenOf(FlowScriptSchema schema, FlowStep? parent)
        {
            return schema.Steps
                .Where(x => x.ParentFlowStep == parent)
                .OrderBy(x => x.OrderNumber);
        }
    }
}
