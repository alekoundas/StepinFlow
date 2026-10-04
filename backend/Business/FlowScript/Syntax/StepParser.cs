using Core.Enums;
using Core.Models.Database;
using Business.FlowScript.Catalogs;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models.Text;
using Business.FlowScript.Models.Binding;

namespace Business.FlowScript.Syntax
{
    /// <summary>
    /// Every Script line that contains a Step information is parsed here.
    /// </summary>
    internal static class StepParser
    {
        public static void Read(FlowScriptSchema document, ScriptLine line, List<string> pendingComments)
        {
            // Get first keyword of the line.
            ScriptKeyword? keyword = SyntaxFacts.ReadFirstKeyword(line.Tokens);
            if (keyword == null)
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.STEP_UNKNOWN, line.Number, line.Tokens[0].Column,
                    $"\"{line.Word(0)}\" is not a step. See FLOW-FORMAT.md for the keywords."));

                pendingComments.Clear();
                return;
            }

            FlowStepTypeEnum type = keyword.As<FlowStepTypeEnum>()!.Value;
            ScriptLineReader reader = new ScriptLineReader(document, line, keyword.TokenCount);

            // Add Success or Failure step.
            if (type == FlowStepTypeEnum.SUCCESS || type == FlowStepTypeEnum.FAILURE)
            {
                AddToSchemaBindng(document, line, new FlowStep { FlowStepType = type });
                reader.End();
                return;
            }

            // Add Stage marker step. Its name is the rest of the line as written, not tokens.
            if (type == FlowStepTypeEnum.MARKER)
            {
                FlowStep flowStep = new FlowStep
                {
                    FlowStepType = FlowStepTypeEnum.MARKER,
                    Name = line.TextAfter(keyword.Text),
                };
                AddToSchemaBindng(document, line, flowStep);

                if (SyntaxFacts.HasTextDelimiter(flowStep.Name))
                    reader.Fail(DiagnosticCodeEnum.TEXT_DELIMITER, $"A stage's name can't contain \"{SyntaxFacts.Symbol(ScriptSymbolEnum.TEXT_START)}\" or \"{SyntaxFacts.Symbol(ScriptSymbolEnum.TEXT_END)}\".");

                pendingComments.Clear();
                return;
            }

            FlowStep step = new FlowStep
            {
                FlowStepType = type,
                CodeComment = string.Join("\n", pendingComments),
            };

            // See the "Modifier" property of the keyword.
            SearchModeEnum? searchMode = keyword.As<SearchModeEnum>();
            if (searchMode != null)
                step.SearchMode = searchMode.Value;

            KeyboardInputTypeEnum? keyboardInputType = keyword.As<KeyboardInputTypeEnum>();
            if (keyboardInputType != null)
                step.KeyboardInputType = keyboardInputType.Value;

            RunCommandPresetEnum? runCommandPreset = keyword.As<RunCommandPresetEnum>();
            if (runCommandPreset != null)
                step.RunCommandPreset = runCommandPreset.Value;

            pendingComments.Clear();

            FlowStepSchemaBindng parsed = AddToSchemaBindng(document, line, step);

            ReadArguments(reader, parsed);
            reader.End();
        }

        // ================================================================
        // Private methods
        // ================================================================

        private static FlowStepSchemaBindng AddToSchemaBindng(FlowScriptSchema document, ScriptLine line, FlowStep step)
        {
            int? parentIndex = FindParentStepIndex(document, line.LeadingSpaces);

            int deepest;
            if (parentIndex == null)
                deepest = 0;
            else
                deepest = document.Steps[parentIndex.Value].LeadingSpaces + 1;

            if (line.LeadingSpaces > deepest)
            {
                document.Diagnostics.Add(Diagnostic.Error(DiagnosticCodeEnum.INDENT_UNEXPECTED, line.Number, line.Tokens[0].Column,
                    $"Indented too far. Each level is one space, so this line can have at most {deepest}."));
            }

            step.OrderNumber = document.Steps.Count;

            FlowStepSchemaBindng parsed = new FlowStepSchemaBindng { Step = step, Line = line.Number, LeadingSpaces = line.LeadingSpaces, ParentIndex = parentIndex };
            document.Steps.Add(parsed);

            return parsed;
        }

        // The steps still open are the previous one and its ancestors, so the parent is the first of them shallower than this line.
        private static int? FindParentStepIndex(FlowScriptSchema document, int indent)
        {
            int? stepIndex;
            if (document.Steps.Count > 0)
                stepIndex = document.Steps.Count - 1;
            else
                return null;

            while (stepIndex != null && document.Steps[stepIndex.Value].LeadingSpaces >= indent)
            {
                stepIndex = document.Steps[stepIndex.Value].ParentIndex;
            }

            return stepIndex;
        }


        // ================================================================
        // Private methods - arguments
        // ================================================================

        private static void ReadArguments(ScriptLineReader reader, FlowStepSchemaBindng parsed)
        {
            FlowStep step = parsed.Step;

            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.SEARCH_IMAGE:
                case FlowStepTypeEnum.SEARCH_TEXT:
                    ReadSearch(reader, parsed);
                    break;

                case FlowStepTypeEnum.CHECK_VALUE:
                    ReadCheckValue(reader, parsed);
                    break;

                case FlowStepTypeEnum.CURSOR_CLICK:
                case FlowStepTypeEnum.CURSOR_RELOCATE:
                case FlowStepTypeEnum.CURSOR_DRAG:
                    ReadCursor(reader, parsed);
                    break;

                case FlowStepTypeEnum.CURSOR_SCROLL:
                    ReadScroll(reader, parsed);
                    break;

                case FlowStepTypeEnum.KEYBOARD_INPUT:
                    ReadKeyboard(reader, step);
                    break;

                case FlowStepTypeEnum.WAIT:
                    ReadWait(reader, step);
                    break;

                case FlowStepTypeEnum.LOOP:
                    ReadLoop(reader, parsed);
                    break;

                case FlowStepTypeEnum.GO_TO:
                    if (reader.Expect("to", DiagnosticCodeEnum.TARGET_MISSING, "Expected \"to\" and the step to go to."))
                        parsed.ReferenceName = reader.Text("the step to go to");
                    break;

                case FlowStepTypeEnum.SUB_FLOW:
                    parsed.SubFlowPath = reader.Text("the sub-flow's path");
                    break;

                case FlowStepTypeEnum.NOTIFY:
                    if (reader.Text("the message") is string message)
                        step.Message = message;
                    break;

                case FlowStepTypeEnum.END_EXECUTION:
                    ReadEndExecution(reader, step);
                    break;

                case FlowStepTypeEnum.SYSTEM_ACTION:
                    ReadSystemAction(reader, step);
                    break;

                case FlowStepTypeEnum.SYSTEM_COMMAND:
                    ReadCommand(reader, step);
                    break;

                case FlowStepTypeEnum.WINDOW_FOCUS:
                case FlowStepTypeEnum.WINDOW_RESIZE:
                case FlowStepTypeEnum.WINDOW_RELOCATE:
                    ReadWindow(reader, parsed);
                    break;

                default:
                    if (reader.Text("the step's name") is string name)
                        step.Name = name;
                    break;
            }
        }

        // The search steps take their clauses in any order, so they are read as clauses rather
        // than by position: template, accuracy, required, match, in, keep, timeout.
        private static void ReadSearch(ScriptLineReader reader, FlowStepSchemaBindng parsed)
        {
            FlowStep step = parsed.Step;
            if (reader.Text("the step's name") is string name)
                step.Name = name;

            if (step.FlowStepType == FlowStepTypeEnum.SEARCH_TEXT && !ReadCondition(reader, step, SyntaxFacts.Quote("text"), "contains"))
                return;

            // A template with no accuracy starts on its mode's default, and "match" may come after it.
            List<ScriptTemplateImage> withoutAccuracy = new List<ScriptTemplateImage>();

            while (reader.HasMore)
            {
                if (reader.Take("template"))
                {
                    if (reader.Text("the template's file name") is not string fileName)
                        return;

                    ScriptTemplateImage template = new ScriptTemplateImage { FileName = fileName };
                    parsed.Templates.Add(template);
                    withoutAccuracy.Add(template);
                }
                else if (reader.Take("in"))
                {
                    parsed.AreaName = reader.Text("the area's name");
                }
                // Belongs to the template written just before it.
                else if (reader.Is("accuracy"))
                {
                    if (parsed.Templates.Count == 0)
                    {
                        reader.Fail(DiagnosticCodeEnum.CLAUSE_WITHOUT_TEMPLATE, "\"accuracy\" belongs after the template it applies to.");
                        return;
                    }

                    reader.Skip();
                    if (reader.Float("the accuracy, such as 0.9") is not float accuracy)
                        return;

                    parsed.Templates[^1].Accuracy = accuracy;
                    withoutAccuracy.Remove(parsed.Templates[^1]);
                }
                // So is this. Required turns "any of these" into "all of these", which a
                // reviewer should see on the template it applies to.
                else if (reader.Is("required"))
                {
                    if (parsed.Templates.Count == 0)
                    {
                        reader.Fail(DiagnosticCodeEnum.CLAUSE_WITHOUT_TEMPLATE, "\"required\" belongs after the template it applies to.");
                        return;
                    }

                    reader.Skip();
                    parsed.Templates[^1].IsRequired = true; //[^1] = last item
                }
                else if (reader.Take("match"))
                {
                    ScriptKeyword? mode = null;
                    if (step.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE)
                        mode = reader.Keyword<TemplateMatchModeEnum>();

                    if (mode == null)
                    {
                        reader.Fail(DiagnosticCodeEnum.MATCH_MODE_UNKNOWN, "Expected \"match shape\" or \"match shape and brightness\", on an image search.");
                        return;
                    }

                    step.TemplateMatchMode = mode.As<TemplateMatchModeEnum>()!.Value;
                }
                else if (reader.Take("keep"))
                {
                    if (reader.Text("the pattern to keep") is string pattern)
                        step.ResultExtractPattern = pattern;
                }
                else if (reader.Take("timeout"))
                {
                    if (reader.Milliseconds("how long to wait") is int timeout)
                        step.TimeoutMilliseconds = timeout;
                }
                // "no timeout" is zero, which the engine reads as for ever.
                else if (reader.Take("no"))
                {
                    if (reader.Expect("timeout", DiagnosticCodeEnum.ARGUMENT_EXPECTED, "Expected \"no timeout\"."))
                        step.TimeoutMilliseconds = 0;
                }
                else
                {
                    reader.Fail(DiagnosticCodeEnum.SEARCH_ARGUMENT_UNKNOWN, $"{reader.Current} is not something a search takes.");
                    return;
                }
            }

            foreach (ScriptTemplateImage template in withoutAccuracy)
                template.Accuracy = ScriptTemplateImage.DefaultAccuracy(step.TemplateMatchMode);
        }

        private static void ReadCheckValue(ScriptLineReader reader, FlowStepSchemaBindng parsed)
        {
            FlowStep step = parsed.Step;
            if (reader.Text("the step's name") is string name)
                step.Name = name;

            // The value is written as "{{Step name}}", because at the point of use a step result
            // and an input are the same thing. Empty is a check with nothing chosen yet.
            int column = reader.Column;
            string example = SyntaxFacts.Quote("{{Step name}}");
            if (reader.Text(DiagnosticCodeEnum.ARGUMENT_EXPECTED, $"Expected the value to check, such as {example}.") is not string variable)
                return;

            if (variable.Length > 0)
            {
                if (!variable.StartsWith("{{", StringComparison.Ordinal) || !variable.EndsWith("}}", StringComparison.Ordinal))
                {
                    reader.Fail(DiagnosticCodeEnum.ARGUMENT_EXPECTED, $"Expected the value to check as {example}.", column);
                    return;
                }

                parsed.ReferenceName = variable[2..^2];
            }

            ReadCondition(reader, step, SyntaxFacts.Quote("100"), ">");
        }

        private static bool ReadCondition(ScriptLineReader reader, FlowStep step, string exampleValue, string exampleWord)
        {
            if (reader.HasFailed)
                return false;

            ConditionSyntax? condition = reader.Condition();
            if (condition == null)
            {
                reader.Fail(DiagnosticCodeEnum.CONDITION_MISSING, $"Expected a condition, such as: {exampleWord} {exampleValue}.");
                return false;
            }

            step.ConditionType = condition.Type;
            step.ConditionText = condition.Text;
            step.ConditionTextEnd = condition.TextEnd;
            return true;
        }

        private static void ReadCursor(ScriptLineReader reader, FlowStepSchemaBindng parsed)
        {
            FlowStep step = parsed.Step;

            // Move says "to", click and drag say "at".
            string lead = step.FlowStepType == FlowStepTypeEnum.CURSOR_RELOCATE ? "to" : "at";
            if (!reader.Expect(lead, DiagnosticCodeEnum.TARGET_MISSING, $"Expected \"{lead}\" followed by what to aim at."))
                return;

            if (!ReadTarget(reader, out string? pointName, out string? referenceName))
                return;

            parsed.PointName = pointName;
            parsed.ReferenceName = referenceName;

            if (step.FlowStepType == FlowStepTypeEnum.CURSOR_DRAG)
            {
                if (!reader.Expect("to", DiagnosticCodeEnum.DRAG_TARGET_MISSING, "A drag needs \"to\" and somewhere to drag to."))
                    return;

                if (!ReadTarget(reader, out string? endPoint, out string? endReference))
                    return;

                parsed.PointEndName = endPoint;
                parsed.ReferenceEndName = endReference;
                return;
            }

            if (step.FlowStepType == FlowStepTypeEnum.CURSOR_CLICK)
                ReadButton(reader, step);
        }

        // The side and what the click does, both optional and in either order; left and single when left out.
        private static void ReadButton(ScriptLineReader reader, FlowStep step)
        {
            CursorButtonTypeEnum button = CursorButtonTypeEnum.LEFT_BUTTON;
            CursorButtonActionTypeEnum action = CursorButtonActionTypeEnum.SINGLE_CLICK;

            while (reader.HasMore)
            {
                CursorButtonTypeEnum? side = SyntaxFacts.ReadButtonSide(reader.Peek());
                CursorButtonActionTypeEnum? kind = SyntaxFacts.ReadButtonAction(reader.Peek());

                if (side != null)
                    button = side.Value;
                else if (kind != null)
                    action = kind.Value;
                else
                    break;

                reader.Skip();
            }

            step.CursorButtonType = button;
            step.CursorButtonActionType = action;
        }

        // point <[ X ]> | <[ a step name ]> | match
        private static bool ReadTarget(ScriptLineReader reader, out string? pointName, out string? referenceName)
        {
            pointName = null;
            referenceName = null;

            if (reader.Take("point"))
            {
                pointName = reader.Text("the point's name");
                return pointName != null;
            }

            // "match" is the current item of a loop, and is neither a point nor a step.
            if (reader.Take("match"))
                return true;

            string example = SyntaxFacts.Quote("a step");
            referenceName = reader.Text(DiagnosticCodeEnum.TARGET_MISSING, $"Expected what to aim at: {example}, point and its name, or match.");
            return referenceName != null;
        }

        private static void ReadScroll(ScriptLineReader reader, FlowStepSchemaBindng parsed)
        {
            CursorScrollDirectionTypeEnum? direction = SyntaxFacts.ReadScrollDirection(reader.Peek());
            if (direction == null)
            {
                reader.Fail(DiagnosticCodeEnum.SCROLL_DIRECTION_UNKNOWN, "Expected up, down, left or right.");
                return;
            }

            reader.Skip();
            parsed.Step.CursorScrollDirectionType = direction.Value;

            if (reader.Integer("how many times to scroll") is not int count)
                return;

            parsed.Step.LoopCount = count;

            if (reader.Take("in"))
                parsed.AreaName = reader.Text("the area's name");
        }

        private static void ReadKeyboard(ScriptLineReader reader, FlowStep step)
        {
            // A combination is written bare - Ctrl+C - and text in delimiters.
            string? keys;
            if (step.KeyboardInputType == KeyboardInputTypeEnum.COMBINATION)
                keys = reader.Word("the keys to press, such as Ctrl+C");
            else
                keys = reader.Text("the text to type");

            if (keys != null)
                step.KeyboardInputText = keys;
        }

        private static void ReadWait(ScriptLineReader reader, FlowStep step)
        {
            if (reader.Milliseconds("how long to wait") is not int shortest)
                return;

            step.WaitForMilliseconds = shortest;

            if (reader.Take("to") && reader.Milliseconds("the longest wait") is int longest)
                step.WaitForMillisecondsMax = longest;
        }

        private static void ReadLoop(ScriptLineReader reader, FlowStepSchemaBindng parsed)
        {
            string malformed = "Expected \"5 times\", \"forever\", or \"each match in\" a search.";

            if (reader.Take("forever"))
            {
                parsed.Step.IsLoopInfinite = true;
                return;
            }

            if (reader.Take("each"))
            {
                if (reader.Expect("match", DiagnosticCodeEnum.LOOP_MALFORMED, malformed) && reader.Expect("in", DiagnosticCodeEnum.LOOP_MALFORMED, malformed))
                    parsed.ReferenceName = reader.Text("the search whose matches to loop over");
                return;
            }

            if (reader.Peek(1) == "times")
            {
                if (reader.Integer("how many times to loop") is int count)
                    parsed.Step.LoopCount = count;

                reader.Take("times");
                return;
            }

            reader.Fail(DiagnosticCodeEnum.LOOP_MALFORMED, malformed);
        }

        private static void ReadEndExecution(ScriptLineReader reader, FlowStep step)
        {
            if (reader.Take("passed"))
            {
                step.EndExecutionAsSuccess = true;
                return;
            }

            if (reader.Take("failed"))
            {
                step.EndExecutionAsSuccess = false;
                if (reader.Text("the reason it failed") is string reason)
                    step.Message = reason;
                return;
            }

            reader.Fail(DiagnosticCodeEnum.ARGUMENT_EXPECTED, "Expected \"passed\", or \"failed\" and the reason.");
        }

        private static void ReadSystemAction(ScriptLineReader reader, FlowStep step)
        {
            if (SyntaxFacts.TryReadName(reader.Peek(), out SystemActionTypeEnum action))
            {
                step.SystemActionType = action;
                reader.Skip();
                return;
            }

            reader.Fail(DiagnosticCodeEnum.SYSTEM_ACTION_UNKNOWN, $"{reader.Current} is not a system action.");
        }

        private static void ReadCommand(ScriptLineReader reader, FlowStep step)
        {
            // Launch already carries its preset from the keyword; Run may name one before the value.
            if (step.RunCommandPreset == RunCommandPresetEnum.CUSTOM && SyntaxFacts.TryReadName(reader.Peek(), out RunCommandPresetEnum preset))
            {
                step.RunCommandPreset = preset;
                reader.Skip();
            }

            if (reader.Text("the command") is string command)
                step.RunCommandValue = command;
        }

        private static void ReadWindow(ScriptLineReader reader, FlowStepSchemaBindng parsed)
        {
            FlowStep step = parsed.Step;

            if (!reader.Expect("process", DiagnosticCodeEnum.PROCESS_MISSING, "Expected \"process\" and the process name."))
                return;

            if (reader.Text("the process name") is not string process)
                return;

            step.ProcessName = process;

            if (reader.Take("title"))
            {
                ScriptKeyword? match = reader.Keyword<TitleMatchModeEnum>();
                if (match == null)
                {
                    reader.Fail(DiagnosticCodeEnum.TITLE_MATCH_UNKNOWN, "Expected is, contains, starts with or matches after \"title\".");
                    return;
                }

                step.TitleMatchMode = match.As<TitleMatchModeEnum>()!.Value;
                if (reader.Text("the title to match") is not string title)
                    return;

                step.TitlePattern = title;
            }

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RESIZE)
            {
                if (reader.Expect("size", DiagnosticCodeEnum.ARGUMENT_EXPECTED, "Expected \"size\" and the width and height.")
                    && reader.Integer("the width") is int width
                    && reader.Integer("the height") is int height)
                {
                    step.WindowWidth = width;
                    step.WindowHeight = height;
                }

                return;
            }

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RELOCATE)
            {
                if (!reader.Expect("to", DiagnosticCodeEnum.TARGET_MISSING, "Expected \"to\" and where to move the window."))
                    return;

                if (ReadTarget(reader, out string? pointName, out string? referenceName))
                {
                    parsed.PointName = pointName;
                    parsed.ReferenceName = referenceName;
                }
            }
        }
    }
}
