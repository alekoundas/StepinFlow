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
    /// Which parser reads a line - from the section it is in and the keyword it opens with - and the
    /// one place the schema is written.
    ///
    /// A line's grammar is its parser's. What the lines mean together is here: the tree the
    /// indentation draws, a template described twice, a comment belonging to the step below, and
    /// every name linked to the row declared above it the moment its line is read.
    /// </summary>
    internal sealed class ScriptLineParser
    {
        private readonly FlowScriptSchema _flowScriptSchema;
        private readonly Dictionary<ScriptToken, string> _comments = new Dictionary<ScriptToken, string>(); // Comments for the step below, by their # token.
        private ScriptSymbolEnum? _section; // Null until the first header fields of flow are read.

        // What the lines above declared. Steps, areas, points and inputs share one set of names.
        private readonly HashSet<string> _names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FlowArea> _areas = new Dictionary<string, FlowArea>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FlowPoint> _points = new Dictionary<string, FlowPoint>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FlowStep> _steps = new Dictionary<string, FlowStep>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, (FlowStepTemplate Facts, bool HasClick)> _templates = new Dictionary<string, (FlowStepTemplate Facts, bool HasClick)>(StringComparer.Ordinal);

        // The last step and the steps it sits in, innermost last, with the indent each was written at.
        private readonly List<(FlowStep Step, int Indent)> _openSteps = new List<(FlowStep Step, int Indent)>();

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
            FlowArea area = new AreaParser(line.Tokens).Parse();
            area.ParentFlowArea = Link(_areas, area.ParentFlowArea?.Name, "area", line.Tokens);

            _flowScriptSchema.Areas.Add(area);

            if (Declare(area.Name, line.Tokens))
                _areas[area.Name] = area;
        }

        private void AddPoint(ScriptLine line)
        {
            FlowPoint point = new PointParser(line.Tokens).Parse();
            point.FlowArea = Link(_areas, point.FlowArea?.Name, "area", line.Tokens);

            _flowScriptSchema.Points.Add(point);

            if (Declare(point.Name, line.Tokens))
                _points[point.Name] = point;
        }

        private void AddInput(ScriptLine line)
        {
            FlowCsvColumn input = new CsvColumnParser(line.Tokens).Parse();
            input.OrderNumber = _flowScriptSchema.Inputs.Count;

            _flowScriptSchema.Inputs.Add(input);
            Declare(input.Name, line.Tokens);
        }

        private void AddTemplate(ScriptLine line)
        {
            (FlowStepTemplate Facts, bool HasClick) template = new TemplateParser(line.Tokens).Parse();

            if (!_templates.TryAdd(template.Facts.Name, template))
                _flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.TEMPLATE_DUPLICATE, line.Number, line.Tokens[0].Column, $"\"{template.Facts.Name}\" is already described above."));
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

            // Linked before its own name is declared, so a step cannot name itself.
            Link(step, line.Tokens);
            JoinTemplateFacts(step);

            // Attach any comments.
            step.CodeComment = string.Join("\n", _comments.Values);
            _comments.Clear();

            AddToTree(line, step);

            if (Declare(step.Name, line.Tokens))
                _steps[step.Name] = step;
        }

        private static FlowStep ParseStep(FlowStepTypeEnum type, IReadOnlyList<ScriptToken> tokens)
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

                case FlowStepTypeEnum.GO_BACK:
                    return new GoBackParser(tokens).Parse();

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

        // The parent is the nearest open step written less deep than this line. Too deep is
        // reported but the step is kept, so the lines below still find their parent.
        private void AddToTree(ScriptLine line, FlowStep step)
        {
            while (_openSteps.Count > 0 && _openSteps[^1].Indent >= line.LeadingSpaces)
                _openSteps.RemoveAt(_openSteps.Count - 1);

            FlowStep? parent = null;
            int deepest = 0;
            if (_openSteps.Count > 0)
            {
                parent = _openSteps[^1].Step;
                deepest = _openSteps[^1].Indent + 1;
            }

            if (line.LeadingSpaces > deepest)
            {
                _flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.INDENT_UNEXPECTED, line.Number, line.Tokens[0].Column,
                    $"Indented too far. Each level is one space, so this line can have at most {deepest}."));
            }

            step.ParentFlowStep = parent;
            step.OrderNumber = _flowScriptSchema.Steps.Count(x => x.ParentFlowStep == parent);

            _flowScriptSchema.Steps.Add(step);
            _openSteps.Add((step, line.LeadingSpaces));
        }


        // ================================================================
        // Private methods - names
        // ================================================================

        // Every row the step names, swapped for the one declared above. A sub-flow is another file,
        // so its path is kept as written instead.
        private void Link(FlowStep step, IReadOnlyList<ScriptToken> tokens)
        {
            step.FlowArea = Link(_areas, step.FlowArea?.Name, "area", tokens);
            step.FlowPoint = Link(_points, step.FlowPoint?.Name, "point", tokens);
            step.FlowPointEnd = Link(_points, step.FlowPointEnd?.Name, "point", tokens);
            step.FlowStepReference = Link(_steps, step.FlowStepReference?.Name, "step", tokens);
            step.FlowStepReferenceEnd = Link(_steps, step.FlowStepReferenceEnd?.Name, "step", tokens);

            if (step.SubFlow != null)
            {
                _flowScriptSchema.SubFlowPaths[step] = step.SubFlow.Name;
                step.SubFlow = null;
            }
        }

        private T? Link<T>(Dictionary<string, T> declared, string? name, string what, IReadOnlyList<ScriptToken> tokens) where T : class
        {
            if (name == null)
                return null;

            if (declared.TryGetValue(name, out T? row))
                return row;

            ScriptToken token = TokenOf(name, tokens);
            _flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.NAME_UNKNOWN, token.Line, token.Column,
                $"Nothing above this line is called \"{name}\", so there is no {what} to point at."));

            return null;
        }

        // False when the name is taken, which is reported at the second one.
        private bool Declare(string name, IReadOnlyList<ScriptToken> tokens)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (_names.Add(name))
                return true;

            ScriptToken token = TokenOf(name, tokens);
            _flowScriptSchema.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.NAME_DUPLICATE, token.Line, token.Column,
                $"\"{name}\" is already used above. Steps, areas, points and inputs share one set of names, because the script refers to them by name."));

            return false;
        }

        // The header's facts about each picture a step names. A template the header gives no click
        // is left for the importer to centre.
        private void JoinTemplateFacts(FlowStep step)
        {
            foreach (FlowStepTemplate template in step.FlowStepTemplates)
            {
                if (!_templates.TryGetValue(template.Name, out (FlowStepTemplate Facts, bool HasClick) header))
                {
                    _flowScriptSchema.TemplatesWithoutClick.Add(template.Name);
                    continue;
                }

                if (!header.HasClick)
                    _flowScriptSchema.TemplatesWithoutClick.Add(template.Name);

                template.ClickOffsetX = header.Facts.ClickOffsetX;
                template.ClickOffsetY = header.Facts.ClickOffsetY;
                template.AuthoredFlowAreaWidth = header.Facts.AuthoredFlowAreaWidth;
                template.AuthoredFlowAreaHeight = header.Facts.AuthoredFlowAreaHeight;
                template.AuthoredDpi = header.Facts.AuthoredDpi;
            }
        }

        // Where a name was written, for the message to point at. A step's result is written {{name}}.
        private static ScriptToken TokenOf(string name, IReadOnlyList<ScriptToken> tokens)
        {
            ScriptToken? token = tokens.FirstOrDefault(x => x.Kind == ScriptTokenKindEnum.QUOTE && (x.Value == name || x.Value == "{{" + name + "}}"));

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
