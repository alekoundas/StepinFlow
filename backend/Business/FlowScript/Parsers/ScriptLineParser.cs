using Business.FlowScript.Catalogs;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models.Binding;
using Business.FlowScript.Models.Text;
using Business.FlowScript.Parsers.Header;
using Business.FlowScript.Parsers.Steps;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers
{
    /// <summary>
    /// Which parser reads a line - from the section it is in and the keyword it opens with - and the
    /// one place the schema is written.
    ///
    /// A line's grammar is its parser's. What the lines mean together is here: the tree the
    /// indentation draws, a template described twice, a comment belonging to the step below.
    /// </summary>
    internal sealed class ScriptLineParser
    {
        private readonly FlowScriptSchema _flowScriptSchema;
        private readonly List<string> _comments = new List<string>(); // Comments for the step below.
        private ScriptSymbolEnum? _section; // Null until the first header fields of flow are read.

        public ScriptLineParser(FlowScriptSchema flowScriptSchema)
        {
            _flowScriptSchema = flowScriptSchema;
        }

        /// <summary>
        /// Read the line and add changes to FlowScriptSchema.
        /// </summary>
        public void Parse(ScriptLine line)
        {
            // Skip empty lines.
            if (line.Tokens[0].Kind == ScriptTokenKindEnum.END_OF_LINE)
                return;

            // Read first token to decide the parsers, null if FLOWFIELD_XXXX (header fields).
            ScriptSymbolEnum? symbol = SymbolOf(line.Tokens[0]);

            // Read and keep CodeComments to be consumed by the next FlowStep
            if (symbol == ScriptSymbolEnum.COMMENT)
            {
                _comments.Add(new CommentParser(line.Tokens).Parse());
                return;
            }

            // Change section if symbol == AREAS,POINTS,CSV_COLUMNS,TEMPLATES,STEPS
            if (IsSection(symbol))
            {
                _section = new SectionHeaderParser(line.Tokens).Parse();
                return;
            }

            // Choose actual line parser.
            switch (_section)
            {
                case null:
                    AddFlowField(symbol, line);
                    break;

                case ScriptSymbolEnum.AREAS:
                    AddArea(line);
                    break;

                case ScriptSymbolEnum.POINTS:
                    AddPoint(line);
                    break;

                case ScriptSymbolEnum.CSV_COLUMNS:
                    AddInput(line);
                    break;

                case ScriptSymbolEnum.TEMPLATES:
                    AddTemplate(line);
                    break;

                case ScriptSymbolEnum.STEPS:
                    AddStep(line);
                    break;
            }
        }


        // ================================================================
        // Private methods - header
        // ================================================================

        private void AddFlowField(ScriptSymbolEnum? symbol, ScriptLine line)
        {
            switch (symbol)
            {
                case ScriptSymbolEnum.FLOWFIELD_NAME:
                    _flowScriptSchema.FlowName = new FlowNameParser(line.Tokens).Parse();
                    break;

                case ScriptSymbolEnum.FLOWFIELD_ID:
                    _flowScriptSchema.PublicId = new FlowIdParser(line.Tokens).Parse();
                    break;

                case ScriptSymbolEnum.FLOWFIELD_SIZES:
                    _flowScriptSchema.Viewports.AddRange(new FlowSizesParser(line.Tokens).Parse());
                    break;

                default:
                    throw ScriptSyntaxException.Unexpected(line.Tokens[0], []);
            }
        }

        private void AddArea(ScriptLine line)
        {
            FlowAreaSchemaBindng area = new AreaParser(line.Tokens).Parse();
            area.Line = line.Number;

            _flowScriptSchema.Areas.Add(area);
        }

        private void AddPoint(ScriptLine line)
        {
            FlowPointSchemaBindng point = new PointParser(line.Tokens).Parse();
            point.Line = line.Number;

            _flowScriptSchema.Points.Add(point);
        }

        private void AddInput(ScriptLine line)
        {
            FlowCsvColumn input = new CsvColumnParser(line.Tokens).Parse();
            input.OrderNumber = _flowScriptSchema.Inputs.Count;

            _flowScriptSchema.Inputs.Add(input);
        }

        private void AddTemplate(ScriptLine line)
        {
            FlowStepTemplateSchemaBindng template = new TemplateParser(line.Tokens).Parse();

            if (_flowScriptSchema.Templates.Any(x => string.Equals(x.FileName, template.FileName, StringComparison.Ordinal)))
            {
                _flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.TEMPLATE_DUPLICATE, line.Number, line.Tokens[0].Column, $"\"{template.FileName}\" is already described above."));
                return;
            }

            template.Line = line.Number;
            _flowScriptSchema.Templates.Add(template);
        }


        // ================================================================
        // Private methods - steps
        // ================================================================

        private void AddStep(ScriptLine line)
        {
            // Read first token to choose the actual parser.
            FlowStepTypeEnum? type = null;
            if (line.Tokens[0].Kind == ScriptTokenKindEnum.KEYWORD)
                type = ScriptKeywordCatalog.Get<FlowStepTypeEnum>(line.Tokens[0].Value)?.As<FlowStepTypeEnum>();

            if (type == null)
                throw ScriptSyntaxException.Unexpected(line.Tokens[0], []);

            FlowStepSchemaBindng parsed;
            try
            {
                parsed = ParseStep(type.Value, line.Tokens);
            }
            catch (ScriptSyntaxException)
            {
                // Still add in tree, so the lines under it find their parent.
                AddToSchemaBindng(line, new FlowStepSchemaBindng() { Step = new FlowStep() { FlowStepType = type.Value } });
                throw;
            }

            // Intent is for the line below it, whatever that line is.
            parsed.Step.CodeComment = string.Join("\n", _comments);
            _comments.Clear();

            AddToSchemaBindng(line, parsed);
        }

        private static FlowStepSchemaBindng ParseStep(FlowStepTypeEnum type, IReadOnlyList<ScriptToken> tokens)
        {
            switch (type)
            {
                case FlowStepTypeEnum.SEARCH_IMAGE:
                    return new SearchImageParser(tokens).Parse();

                case FlowStepTypeEnum.SEARCH_TEXT:
                    return new SearchTextParser(tokens).Parse();

                case FlowStepTypeEnum.CHECK_VALUE:
                    return new CheckValueParser(tokens).Parse();

                case FlowStepTypeEnum.CURSOR_CLICK:
                    return new CursorClickParser(tokens).Parse();

                case FlowStepTypeEnum.CURSOR_RELOCATE:
                    return new CursorRelocateParser(tokens).Parse();

                case FlowStepTypeEnum.CURSOR_DRAG:
                    return new CursorDragParser(tokens).Parse();

                case FlowStepTypeEnum.CURSOR_SCROLL:
                    return new CursorScrollParser(tokens).Parse();

                case FlowStepTypeEnum.KEYBOARD_INPUT:
                    return new KeyboardInputParser(tokens).Parse();

                case FlowStepTypeEnum.WAIT:
                    return new WaitParser(tokens).Parse();

                case FlowStepTypeEnum.LOOP:
                    return new LoopParser(tokens).Parse();

                case FlowStepTypeEnum.GO_TO:
                    return new GoToParser(tokens).Parse();

                case FlowStepTypeEnum.SUB_FLOW:
                    return new SubFlowParser(tokens).Parse();

                case FlowStepTypeEnum.NOTIFY:
                    return new NotifyParser(tokens).Parse();

                case FlowStepTypeEnum.END_EXECUTION:
                    return new EndExecutionParser(tokens).Parse();

                case FlowStepTypeEnum.SYSTEM_ACTION:
                    return new SystemActionParser(tokens).Parse();

                case FlowStepTypeEnum.SYSTEM_COMMAND:
                    return new SystemCommandParser(tokens).Parse();

                case FlowStepTypeEnum.WINDOW_FOCUS:
                    return new WindowFocusParser(tokens).Parse();

                case FlowStepTypeEnum.WINDOW_RESIZE:
                    return new WindowResizeParser(tokens).Parse();

                case FlowStepTypeEnum.WINDOW_RELOCATE:
                    return new WindowRelocateParser(tokens).Parse();

                case FlowStepTypeEnum.MARKER:
                    return new MarkerParser(tokens).Parse();

                case FlowStepTypeEnum.SUCCESS:
                case FlowStepTypeEnum.FAILURE:
                    return new BranchParser(tokens).Parse();

                default:
                    throw new InvalidOperationException($"No parser reads a {type} step.");
            }
        }

        // Too deep is reported but the step is kept, so the lines below still find their parent.
        private void AddToSchemaBindng(ScriptLine line, FlowStepSchemaBindng parsed)
        {
            int? parentIndex = FindParentStepIndex(line.LeadingSpaces);

            int deepest;
            if (parentIndex == null)
                deepest = 0;
            else
                deepest = _flowScriptSchema.Steps[parentIndex.Value].LeadingSpaces + 1;

            if (line.LeadingSpaces > deepest)
            {
                _flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.INDENT_UNEXPECTED, line.Number, line.Tokens[0].Column,
                    $"Indented too far. Each level is one space, so this line can have at most {deepest}."));
            }

            parsed.Step.OrderNumber = _flowScriptSchema.Steps.Count;
            parsed.Line = line.Number;
            parsed.LeadingSpaces = line.LeadingSpaces;
            parsed.ParentIndex = parentIndex;

            _flowScriptSchema.Steps.Add(parsed);
        }

        // The steps still open are the previous one and its ancestors, so the parent is the first of them shallower than this line.
        private int? FindParentStepIndex(int indent)
        {
            int? stepIndex;
            if (_flowScriptSchema.Steps.Count > 0)
                stepIndex = _flowScriptSchema.Steps.Count - 1;
            else
                return null;

            while (stepIndex != null && _flowScriptSchema.Steps[stepIndex.Value].LeadingSpaces >= indent)
            {
                stepIndex = _flowScriptSchema.Steps[stepIndex.Value].ParentIndex;
            }

            return stepIndex;
        }


        // ================================================================
        // Private methods - first keyword
        // ================================================================

        // The symbol a line opens with, when it opens with one: a comment, a section, a flow field.
        private static ScriptSymbolEnum? SymbolOf(ScriptToken token)
        {
            if (token.Kind != ScriptTokenKindEnum.KEYWORD)
                return null;

            return ScriptKeywordCatalog.Get<ScriptSymbolEnum>(token.Value)?.As<ScriptSymbolEnum>();
        }

        private static bool IsSection(ScriptSymbolEnum? symbol)
        {
            return symbol == ScriptSymbolEnum.AREAS
                || symbol == ScriptSymbolEnum.POINTS
                || symbol == ScriptSymbolEnum.CSV_COLUMNS
                || symbol == ScriptSymbolEnum.TEMPLATES
                || symbol == ScriptSymbolEnum.STEPS;
        }
    }
}
