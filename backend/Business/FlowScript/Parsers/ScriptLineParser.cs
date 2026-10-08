using Business.FlowScript.Catalogs;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;
using Business.FlowScript.Models.Text;
using Business.FlowScript.Parsers.Header;
using Business.FlowScript.Parsers.Steps;
using Business.FlowScript.Parsers.Structure;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Parsers
{
    /// <summary>
    /// Which parser choosen to read a line is based on
    /// the current section of the script(header,steps,etc) 
    /// and the first token of the line
    /// </summary>
    internal sealed class ScriptLineParser
    {
        private readonly FlowScriptSchema _flowScriptSchema;
        private readonly ScriptScope _scope; // What the lines above declared, for the parsers to resolve names against.
        private readonly Dictionary<ScriptToken, string> _comments = new Dictionary<ScriptToken, string>(); // Comments for the step below, by their # token.
        private ScriptSymbolEnum? _section; // Null until the first header fields of flow are read.
        private readonly List<(FlowStep Step, int LeadingSpaces)> _openSteps = new List<(FlowStep Step, int LeadingSpaces)>(); // Keep parents and last step by indent.

        public ScriptLineParser(FlowScriptSchema flowScriptSchema)
        {
            _flowScriptSchema = flowScriptSchema;
            _scope = new ScriptScope(flowScriptSchema.Diagnostics);
        }

        /// <summary>
        /// Read the line and add changes to FlowScriptSchema.
        /// </summary>
        public void Parse(ScriptLine line)
        {
            // Skip empty lines.
            if (line.Tokens[0].Kind == ScriptTokenKindEnum.END_OF_LINE)
                return;

            // Read first token to decide the parsers: a comment, a section or a flow field (FLOWFIELD_XXXX).
            // null if FLOWFIELD_XXXX.
            ScriptSymbolEnum? symbol = SymbolOf(line.Tokens[0]);

            // Read and keep CodeComments to be consumed by the next FlowStep
            if (symbol == ScriptSymbolEnum.COMMENT)
            {
                _comments.Add(line.Tokens[0], new CommentParser(line.Tokens).Parse());
                return;
            }

            // Only a step takes a comment.
            if (_section != ScriptSymbolEnum.STEPS || IsSection(symbol))
                ReportUnattachedComments();

            // Change section if symbol == AREAS,POINTS,CSV_COLUMNS,TEMPLATES,STEPS
            if (IsSection(symbol))
            {
                _section = new SectionParser(line.Tokens).Parse();
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


        /// <summary>
        /// The end of the file. Check for any leftover comments.
        /// </summary>
        public void Finish()
        {
            ReportUnattachedComments();
        }


        // ================================================================
        // Private methods - comments
        // ================================================================

        // Comments no step came to take. They belong to nothing, so they are reported rather than
        // handed to a step further down that they were not written above.
        private void ReportUnattachedComments()
        {
            if (_comments.Count == 0)
                return;

            ScriptToken first = _comments.Keys.First();
            _flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.COMMENT_UNATTACHED, first.Line, first.Column,
                "A comment belongs to the step below it, and nothing below this one is a step."));

            _comments.Clear();
        }


        // ================================================================
        // Private methods - header
        // ================================================================

        private void AddFlowField(ScriptSymbolEnum? symbol, ScriptLine line)
        {
            switch (symbol)
            {
                case ScriptSymbolEnum.FLOWFIELD_NAME:
                    _flowScriptSchema.Flow.Name = new FlowNameParser(line.Tokens).Parse();
                    break;

                case ScriptSymbolEnum.FLOWFIELD_ID:
                    _flowScriptSchema.Flow.PublicId = new FlowIdParser(line.Tokens).Parse();
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
            FlowArea area = new AreaParser(line.Tokens, _scope).Parse();

            _flowScriptSchema.Areas.Add(area);
            _scope.Declare(area, TokenOf(area.Name, line.Tokens));
        }

        private void AddPoint(ScriptLine line)
        {
            FlowPoint point = new PointParser(line.Tokens, _scope).Parse();

            _flowScriptSchema.Points.Add(point);
            _scope.Declare(point, TokenOf(point.Name, line.Tokens));
        }

        private void AddInput(ScriptLine line)
        {
            FlowCsvColumn input = new CsvColumnParser(line.Tokens).Parse();
            input.OrderNumber = _flowScriptSchema.Inputs.Count;

            _flowScriptSchema.Inputs.Add(input);
            _scope.Declare(input, TokenOf(input.Name, line.Tokens));
        }

        private void AddTemplate(ScriptLine line)
        {
            FlowStepTemplate template = new TemplateParser(line.Tokens).Parse();

            _scope.Declare(template, line.Tokens[0]);
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

            FlowStep step;
            try
            {
                step = ParseStep(type.Value, line.Tokens);
            }
            catch (ScriptSyntaxException)
            {
                // Still add in tree, so the lines under it find their parent.
                AddToTree(line, new FlowStep() { FlowStepType = type.Value });
                throw;
            }

            // A sub-flow is another file, which nothing in this one declares, so its path is kept as
            // written instead of resolved.
            if (step.SubFlow != null)
            {
                _flowScriptSchema.SubFlowPaths[step] = step.SubFlow.Name;
                step.SubFlow = null;
            }

            // Attach any comments.
            step.CodeComment = string.Join("\n", _comments.Values);
            _comments.Clear();

            AddToTree(line, step);

            // Declared after its line is read, so a step cannot name itself.
            _scope.Declare(step, TokenOf(step.Name, line.Tokens));
        }

        private FlowStep ParseStep(FlowStepTypeEnum type, IReadOnlyList<ScriptToken> tokens)
        {
            switch (type)
            {
                case FlowStepTypeEnum.SEARCH_IMAGE:
                    return new SearchImageParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.SEARCH_TEXT:
                    return new SearchTextParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.CHECK_VALUE:
                    return new CheckValueParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.CURSOR_CLICK:
                    return new CursorClickParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.CURSOR_RELOCATE:
                    return new CursorRelocateParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.CURSOR_DRAG:
                    return new CursorDragParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.CURSOR_SCROLL:
                    return new CursorScrollParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.KEYBOARD_INPUT:
                    return new KeyboardInputParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.WAIT:
                    return new WaitParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.LOOP:
                    return new LoopParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.GO_BACK:
                    return new GoBackParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.SUB_FLOW:
                    return new SubFlowParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.NOTIFY:
                    return new NotifyParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.END_EXECUTION:
                    return new EndExecutionParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.SYSTEM_ACTION:
                    return new SystemActionParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.SYSTEM_COMMAND:
                    return new SystemCommandParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.WINDOW_FOCUS:
                    return new WindowFocusParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.WINDOW_RESIZE:
                    return new WindowResizeParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.WINDOW_RELOCATE:
                    return new WindowRelocateParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.STAGE_MARKER:
                    return new StageMarkerParser(tokens, _scope).Parse();

                case FlowStepTypeEnum.SUCCESS:
                case FlowStepTypeEnum.FAILURE:
                    return new BranchParser(tokens, _scope).Parse();

                default:
                    throw new InvalidOperationException($"No parser reads a {type} step.");
            }
        }

        // Places a step in the tree using only its leading spaces
        // The parent is the nearest open step written less deep than this line.
        // Too deep is reported but the step is kept, so the lines below still find their parent.
        private void AddToTree(ScriptLine line, FlowStep step)
        {
            // Remove the last step if last steo was deeper.
            while (_openSteps.Count > 0 && _openSteps[^1].LeadingSpaces >= line.LeadingSpaces)
                _openSteps.RemoveAt(_openSteps.Count - 1);

            FlowStep? parent = null;
            int maxLeadingSpaces = 0;
            if (_openSteps.Count > 0)
            {
                parent = _openSteps[^1].Step;
                maxLeadingSpaces = _openSteps[^1].LeadingSpaces + 1;
            }

            if (line.LeadingSpaces > maxLeadingSpaces)
            {
                _flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.LEADING_SPACES_UNEXPECTED, line.Number, line.Tokens[0].Column,
                    $"Indented too far. Each level is one space, so this line can have at most {maxLeadingSpaces}."));
            }

            step.ParentFlowStep = parent; // Null for the first children of the flow.
            step.OrderNumber = _flowScriptSchema.Steps.Count(x => x.ParentFlowStep == parent);

            _flowScriptSchema.Steps.Add(step);
            _openSteps.Add((step, line.LeadingSpaces));
        }


        // ================================================================
        // Private methods - names
        // ================================================================

        // Where a line's own name was written, for a duplicate to be reported at.
        private static ScriptToken TokenOf(string name, IReadOnlyList<ScriptToken> tokens)
        {
            ScriptToken? token = tokens.FirstOrDefault(x => x.Kind == ScriptTokenKindEnum.QUOTE && x.Value == name);

            return token ?? tokens[0];
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
