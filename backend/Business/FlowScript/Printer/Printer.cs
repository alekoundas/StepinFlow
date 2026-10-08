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
    /// A flow converted to text. Uses a different writer per line. 
    /// The mirror of Scanner.cs 
    /// </summary>
    public sealed class Printer : IPrinter
    {
        public string Write(FlowScriptSchema schema)
        {
            StringBuilder builder = new StringBuilder();

            // Header - Flow fields
            WriteFlowFields(schema, builder);
            builder.AppendLine();

            // Header - Flow relations
            WriteAreas(builder, schema);
            WritePoints(builder, schema);
            WriteCsvColumns(builder, schema);
            WriteTemplates(builder, schema);

            // FlowSteps
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
                    string subFlowPath = schema.SubFlowPaths.GetValueOrDefault(step, string.Empty);
                    return new SubFlowWriter(step, subFlowPath);

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
        // Private methods - header sections
        // ================================================================

        private static void WriteFlowFields(FlowScriptSchema schema, StringBuilder builder)
        {
            string flowName = new FlowNameWriter(schema.Flow).Write();
            builder.AppendLine(flowName);

            string flowId = new FlowIdWriter(schema.Flow).Write();
            builder.AppendLine(flowId);

            if (schema.Viewports.Count > 0)
            {
                string flowSizes = new FlowSizesWriter(schema.Viewports).Write();
                builder.AppendLine(flowSizes);
            }

            // As many lines as it has, one # each, the way a code comment of several lines is written.
            if (!string.IsNullOrWhiteSpace(schema.Flow.Description))
            {
                string heading = new SectionWriter(ScriptSymbolEnum.FLOWFIELD_DESCRIPTION).Write();
                builder.AppendLine(heading);

                foreach (string text in schema.Flow.Description.Split('\n'))
                {
                    string line = new CommentWriter(text).Write();
                    builder.Append("  ").AppendLine(line);
                }
            }
        }

        private static void WriteAreas(StringBuilder builder, FlowScriptSchema schema)
        {
            if (schema.Areas.Count == 0)
                return;

            string heading = new SectionWriter(ScriptSymbolEnum.AREAS).Write();
            builder.AppendLine(heading);

            // Parents before children, so a child's "inside X" always names something already read.
            foreach (FlowArea root in schema.Areas.Where(x => x.ParentFlowArea == null).OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                string rootLine = new AreaWriter(root).Write();
                builder.Append("  ").AppendLine(rootLine);

                foreach (FlowArea child in schema.Areas.Where(x => x.ParentFlowArea == root).OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    string childLine = new AreaWriter(child).Write();
                    builder.Append("  ").AppendLine(childLine);
                }
            }

            builder.AppendLine();
        }

        private static void WritePoints(StringBuilder builder, FlowScriptSchema schema)
        {
            if (schema.Points.Count == 0)
                return;

            string heading = new SectionWriter(ScriptSymbolEnum.POINTS).Write();
            builder.AppendLine(heading);

            foreach (FlowPoint point in schema.Points.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                string line = new PointWriter(point).Write();
                builder.Append("  ").AppendLine(line);
            }

            builder.AppendLine();
        }

        private static void WriteCsvColumns(StringBuilder builder, FlowScriptSchema schema)
        {
            if (schema.Inputs.Count == 0)
                return;

            string heading = new SectionWriter(ScriptSymbolEnum.CSV_COLUMNS).Write();
            builder.AppendLine(heading);

            foreach (FlowCsvColumn input in schema.Inputs.OrderBy(x => x.OrderNumber))
            {
                string line = new CsvColumnWriter(input).Write();
                builder.Append("  ").AppendLine(line);
            }

            builder.AppendLine();
        }

        private static void WriteTemplates(StringBuilder builder, FlowScriptSchema schema)
        {
            List<FlowStepTemplate> templates = schema.Steps
                .SelectMany(x => x.FlowStepTemplates)
                .DistinctBy(x => x.Name, StringComparer.Ordinal)
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToList();

            if (templates.Count == 0)
                return;

            string heading = new SectionWriter(ScriptSymbolEnum.TEMPLATES).Write();
            builder.AppendLine(heading);

            foreach (FlowStepTemplate template in templates)
            {
                string line = new TemplateWriter(template).Write();
                builder.Append("  ").AppendLine(line);
            }

            builder.AppendLine();
        }

       


        // ================================================================
        // Private methods - steps
        // ================================================================

        private static void WriteSteps(StringBuilder builder, FlowScriptSchema schema)
        {
            string heading = new SectionWriter(ScriptSymbolEnum.STEPS).Write();
            builder.AppendLine(heading);

            foreach (FlowStep step in ChildrenOf(schema, null))
            {
                builder.AppendLine();
                WriteStep(builder, schema, step, 0);
            }
        }

        private static void WriteStep(StringBuilder builder, FlowScriptSchema schema, FlowStep step, int leadingSpaces)
        {
            List<FlowStep> children = ChildrenOf(schema, step).ToList();

            // Add branch only when the branch contains a step.
            if (TreeStepHelper.IsBranchChild(step.FlowStepType) && children.Count == 0 && string.IsNullOrWhiteSpace(step.CodeComment))
                return;

            // Get FlowStep line.
            string line = StepWriter(step, schema).Write();

            WriteComment(builder, step, leadingSpaces);
            builder.Append(' ', leadingSpaces).AppendLine(line);

            foreach (FlowStep child in children)
                WriteStep(builder, schema, child, leadingSpaces + 1);
        }

        private static void WriteComment(StringBuilder builder, FlowStep step, int leadingSpaces)
        {
            if (string.IsNullOrWhiteSpace(step.CodeComment))
                return;

            foreach (string text in step.CodeComment.Split('\n'))
            {
                string line = new CommentWriter(text).Write();
                builder.Append(' ', leadingSpaces).AppendLine(line);
            }
        }

        private static IEnumerable<FlowStep> ChildrenOf(FlowScriptSchema schema, FlowStep? parent)
        {
            return schema.Steps
                .Where(x => x.ParentFlowStep == parent)
                .OrderBy(x => x.OrderNumber);
        }
    }
}
