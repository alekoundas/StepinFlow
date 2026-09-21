using System.Globalization;

using Core.Enums;
using Core.Models.Database;

namespace Business.Services.FlowScriptService
{
    /// <summary>
    /// The step half of the reader. Split from the header half because the two share nothing but
    /// the document they fill, and one file holding both is a file nobody reads to the end.
    /// </summary>
    public sealed partial class FlowScriptReader
    {
        private static void ReadStep(
            FlowScriptDocument document,
            ScriptLine line,
            Dictionary<int, int> lastIndexAtIndent,
            List<string> pendingComments,
            ref int order)
        {
            // A branch is a row of its own in the database, and the steps under it hang off it.
            if (line.Raw.Trim() == "Success:" || line.Raw.Trim() == "Failure:")
            {
                FlowStepTypeEnum branchType = line.Raw.Trim() == "Success:"
                    ? FlowStepTypeEnum.SUCCESS
                    : FlowStepTypeEnum.FAILURE;

                Add(document, line, lastIndexAtIndent, new FlowStep { FlowStepType = branchType }, ref order);
                return;
            }

            if (line.IsSection)
            {
                Add(document, line, lastIndexAtIndent, new FlowStep
                {
                    FlowStepType = FlowStepTypeEnum.MARKER,
                    Name = line.TextAfterHash,
                }, ref order);

                pendingComments.Clear();
                return;
            }

            ScriptKeyword? keyword = FlowScriptKeywords.Match(line.Tokens);
            if (keyword == null)
            {
                document.Errors.Add(new FlowScriptError(line.Number, line.Tokens[0].Column,
                    $"\"{line.Word(0)}\" is not a step. See FLOW-FORMAT.md for the keywords."));

                pendingComments.Clear();
                return;
            }

            FlowStep step = new FlowStep
            {
                FlowStepType = keyword.Type,
                CodeComment = string.Join("\n", pendingComments),
            };

            if (keyword.SearchMode != null)
                step.SearchMode = keyword.SearchMode.Value;

            if (keyword.KeyboardInputType != null)
                step.KeyboardInputType = keyword.KeyboardInputType.Value;

            if (keyword.RunCommandPreset != null)
                step.RunCommandPreset = keyword.RunCommandPreset.Value;

            pendingComments.Clear();

            ParsedStep parsed = Add(document, line, lastIndexAtIndent, step, ref order);
            int at = keyword.Text.Split(' ').Length;

            ReadArguments(document, line, parsed, at);
        }

        private static ParsedStep Add(
            FlowScriptDocument document,
            ScriptLine line,
            Dictionary<int, int> lastIndexAtIndent,
            FlowStep step,
            ref int order)
        {
            // One rule covers both shapes: a branch row sits one in from its step, a container's
            // children one in from the container, so the parent is whatever was last seen outside.
            int? parentIndex = null;
            if (line.Indent > 0 && lastIndexAtIndent.TryGetValue(line.Indent - 1, out int found))
                parentIndex = found;

            step.OrderNumber = order++;

            ParsedStep parsed = new ParsedStep { Step = step, Line = line.Number, ParentIndex = parentIndex };
            document.Steps.Add(parsed);

            int index = document.Steps.Count - 1;
            lastIndexAtIndent[line.Indent] = index;

            // Anything deeper belonged to a subtree this line has just closed.
            foreach (int deeper in lastIndexAtIndent.Keys.Where(x => x > line.Indent).ToList())
                lastIndexAtIndent.Remove(deeper);

            return parsed;
        }


        // ================================================================
        // Private methods - arguments
        // ================================================================

        private static void ReadArguments(FlowScriptDocument document, ScriptLine line, ParsedStep parsed, int at)
        {
            FlowStep step = parsed.Step;

            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.SEARCH_IMAGE:
                case FlowStepTypeEnum.SEARCH_TEXT:
                    ReadSearch(document, line, parsed, at);
                    break;

                case FlowStepTypeEnum.CHECK_VALUE:
                    ReadCheckValue(document, line, parsed, at);
                    break;

                case FlowStepTypeEnum.CURSOR_CLICK:
                case FlowStepTypeEnum.CURSOR_RELOCATE:
                case FlowStepTypeEnum.CURSOR_DRAG:
                    ReadCursor(document, line, parsed, at);
                    break;

                case FlowStepTypeEnum.CURSOR_SCROLL:
                    ReadScroll(document, line, parsed, at);
                    break;

                case FlowStepTypeEnum.KEYBOARD_INPUT:
                    step.KeyboardInputText = line.Word(at);
                    break;

                case FlowStepTypeEnum.WAIT:
                    ReadWait(document, line, parsed, at);
                    break;

                case FlowStepTypeEnum.LOOP:
                    ReadLoop(document, line, parsed, at);
                    break;

                case FlowStepTypeEnum.GO_TO:
                    parsed.ReferenceName = line.Word(at + 1);
                    break;

                case FlowStepTypeEnum.SUB_FLOW:
                    parsed.SubFlowPath = line.Word(at);
                    break;

                case FlowStepTypeEnum.NOTIFY:
                    step.Message = line.Word(at);
                    break;

                case FlowStepTypeEnum.END_EXECUTION:
                    step.EndExecutionAsSuccess = line.Word(at) == "passed";
                    step.Message = step.EndExecutionAsSuccess ? string.Empty : line.Word(at + 1);
                    break;

                case FlowStepTypeEnum.SYSTEM_ACTION:
                    ReadSystemAction(document, line, parsed, at);
                    break;

                case FlowStepTypeEnum.SYSTEM_COMMAND:
                    ReadCommand(line, parsed, at);
                    break;

                case FlowStepTypeEnum.WINDOW_FOCUS:
                case FlowStepTypeEnum.WINDOW_RESIZE:
                case FlowStepTypeEnum.WINDOW_RELOCATE:
                    ReadWindow(document, line, parsed, at);
                    break;

                default:
                    step.Name = line.Word(at);
                    break;
            }
        }

        // The search steps take their clauses in any order, so they are read as clauses rather
        // than by position: template, in, accuracy, keep, timeout.
        private static void ReadSearch(FlowScriptDocument document, ScriptLine line, ParsedStep parsed, int at)
        {
            FlowStep step = parsed.Step;
            step.Name = line.Word(at);

            int i = at + 1;

            if (step.FlowStepType == FlowStepTypeEnum.SEARCH_TEXT)
            {
                ParsedCondition? condition = Words.ReadCondition(line.Tokens, i);
                if (condition == null)
                {
                    document.Errors.Add(new FlowScriptError(line.Number, Column(line, i), "Expected a condition, such as: contains \"text\"."));
                    return;
                }

                step.ConditionType = condition.Type;
                step.ConditionText = condition.Text;
                step.ConditionTextEnd = condition.TextEnd;
                i += condition.Words;
            }

            while (i < line.Tokens.Count)
            {
                string word = line.Word(i);

                switch (word)
                {
                    case "template":
                        parsed.TemplateFileNames.Add(line.Word(i + 1));
                        i += 2;
                        break;

                    case "in":
                        parsed.AreaName = line.Word(i + 1);
                        i += 2;
                        break;

                    case "accuracy":
                        step.Accuracy = Float(line.Word(i + 1));
                        i += 2;
                        break;

                    case "keep":
                        step.ResultExtractPattern = line.Word(i + 1);
                        i += 2;
                        break;

                    case "timeout":
                        step.TimeoutMilliseconds = Milliseconds(line.Word(i + 1));
                        i += 2;
                        break;

                    case "no":
                        // "no timeout" is zero, which the engine reads as for ever.
                        step.TimeoutMilliseconds = 0;
                        i += 2;
                        break;

                    default:
                        document.Errors.Add(new FlowScriptError(line.Number, Column(line, i), $"\"{word}\" is not something a search takes."));
                        return;
                }
            }
        }

        private static void ReadCheckValue(FlowScriptDocument document, ScriptLine line, ParsedStep parsed, int at)
        {
            FlowStep step = parsed.Step;
            step.Name = line.Word(at);

            // The value is written as "{{Step name}}", because at the point of use a step result
            // and an input are the same thing.
            string variable = line.Word(at + 1);
            if (variable.StartsWith("{{", StringComparison.Ordinal) && variable.EndsWith("}}", StringComparison.Ordinal))
                parsed.ReferenceName = variable[2..^2];

            ParsedCondition? condition = Words.ReadCondition(line.Tokens, at + 2);
            if (condition == null)
            {
                document.Errors.Add(new FlowScriptError(line.Number, Column(line, at + 2), "Expected a condition, such as: > \"100\"."));
                return;
            }

            step.ConditionType = condition.Type;
            step.ConditionText = condition.Text;
            step.ConditionTextEnd = condition.TextEnd;
        }

        private static void ReadCursor(FlowScriptDocument document, ScriptLine line, ParsedStep parsed, int at)
        {
            FlowStep step = parsed.Step;

            // Move says "to", click and drag say "at".
            string lead = step.FlowStepType == FlowStepTypeEnum.CURSOR_RELOCATE ? "to" : "at";
            if (line.Word(at) != lead)
            {
                document.Errors.Add(new FlowScriptError(line.Number, Column(line, at), $"Expected \"{lead}\" followed by what to aim at."));
                return;
            }

            int i = ReadTarget(line, at + 1, out string? pointName, out string? referenceName);
            parsed.PointName = pointName;
            parsed.ReferenceName = referenceName;

            if (step.FlowStepType == FlowStepTypeEnum.CURSOR_DRAG)
            {
                if (line.Word(i) != "to")
                {
                    document.Errors.Add(new FlowScriptError(line.Number, Column(line, i), "A drag needs \"to\" and somewhere to drag to."));
                    return;
                }

                ReadTarget(line, i + 1, out string? endPoint, out string? endReference);
                parsed.PointEndName = endPoint;
                parsed.ReferenceEndName = endReference;
                return;
            }

            if (step.FlowStepType == FlowStepTypeEnum.CURSOR_CLICK)
            {
                List<string> rest = line.Tokens.Skip(i).Select(x => x.Text).ToList();
                (CursorButtonTypeEnum button, CursorButtonActionTypeEnum action) = Words.ReadButton(rest);

                step.CursorButtonType = button;
                step.CursorButtonActionType = action;
            }
        }

        // point "X" | "a step name" | match
        private static int ReadTarget(ScriptLine line, int at, out string? pointName, out string? referenceName)
        {
            pointName = null;
            referenceName = null;

            if (line.Word(at) == "point" && !line.Tokens[at].WasQuoted)
            {
                pointName = line.Word(at + 1);
                return at + 2;
            }

            // "match" is the current item of a loop, and is neither a point nor a step.
            if (line.Word(at) == "match" && !line.Tokens[at].WasQuoted)
                return at + 1;

            referenceName = line.Word(at);
            return at + 1;
        }

        private static void ReadScroll(FlowScriptDocument document, ScriptLine line, ParsedStep parsed, int at)
        {
            CursorScrollDirectionTypeEnum? direction = Words.ReadScrollDirection(line.Word(at));
            if (direction == null)
            {
                document.Errors.Add(new FlowScriptError(line.Number, Column(line, at), "Expected up, down, left or right."));
                return;
            }

            parsed.Step.CursorScrollDirectionType = direction.Value;
            parsed.Step.LoopCount = Integer(line.Word(at + 1));

            if (line.Word(at + 2) == "in")
                parsed.AreaName = line.Word(at + 3);
        }

        private static void ReadWait(FlowScriptDocument document, ScriptLine line, ParsedStep parsed, int at)
        {
            parsed.Step.WaitForMilliseconds = Milliseconds(line.Word(at));

            if (line.Word(at + 1) == "to")
                parsed.Step.WaitForMillisecondsMax = Milliseconds(line.Word(at + 2));

            if (parsed.Step.WaitForMilliseconds == 0 && line.Word(at) != "0ms")
                document.Errors.Add(new FlowScriptError(line.Number, Column(line, at), $"\"{line.Word(at)}\" is not a duration. Expected something like 800ms."));
        }

        private static void ReadLoop(FlowScriptDocument document, ScriptLine line, ParsedStep parsed, int at)
        {
            if (line.Word(at) == "forever")
            {
                parsed.Step.IsLoopInfinite = true;
                return;
            }

            if (line.Word(at) == "each" && line.Word(at + 1) == "match" && line.Word(at + 2) == "in")
            {
                parsed.ReferenceName = line.Word(at + 3);
                return;
            }

            if (line.Word(at + 1) == "times")
            {
                parsed.Step.LoopCount = Integer(line.Word(at));
                return;
            }

            document.Errors.Add(new FlowScriptError(line.Number, Column(line, at), "Expected \"5 times\", \"forever\", or \"each match in\" a search."));
        }

        private static void ReadSystemAction(FlowScriptDocument document, ScriptLine line, ParsedStep parsed, int at)
        {
            if (Enum.TryParse(line.Word(at), out SystemActionTypeEnum action))
            {
                parsed.Step.SystemActionType = action;
                return;
            }

            document.Errors.Add(new FlowScriptError(line.Number, Column(line, at), $"\"{line.Word(at)}\" is not a system action."));
        }

        private static void ReadCommand(ScriptLine line, ParsedStep parsed, int at)
        {
            // Launch already carries its preset from the keyword; Run may name one before the value.
            if (parsed.Step.RunCommandPreset == RunCommandPresetEnum.CUSTOM
                && !line.Tokens[at].WasQuoted
                && Enum.TryParse(line.Word(at), out RunCommandPresetEnum preset))
            {
                parsed.Step.RunCommandPreset = preset;
                at++;
            }

            parsed.Step.RunCommandValue = line.Word(at);
        }

        private static void ReadWindow(FlowScriptDocument document, ScriptLine line, ParsedStep parsed, int at)
        {
            FlowStep step = parsed.Step;

            if (line.Word(at) != "process")
            {
                document.Errors.Add(new FlowScriptError(line.Number, Column(line, at), "Expected \"process\" and the process name."));
                return;
            }

            step.ProcessName = line.Word(at + 1);
            int i = at + 2;

            if (line.Word(i) == "title")
            {
                (TitleMatchModeEnum Mode, int Words)? match = Words.ReadTitleMatch(line.Tokens, i + 1);
                if (match == null)
                {
                    document.Errors.Add(new FlowScriptError(line.Number, Column(line, i + 1), "Expected is, contains, starts with or matches after \"title\"."));
                    return;
                }

                step.TitleMatchMode = match.Value.Mode;
                step.TitlePattern = line.Word(i + 1 + match.Value.Words);
                i += 2 + match.Value.Words;
            }

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RESIZE && line.Word(i) == "size")
            {
                step.WindowWidth = Integer(line.Word(i + 1));
                step.WindowHeight = Integer(line.Word(i + 2));
                return;
            }

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RELOCATE && line.Word(i) == "to")
            {
                ReadTarget(line, i + 1, out string? pointName, out string? referenceName);
                parsed.PointName = pointName;
                parsed.ReferenceName = referenceName;
            }
        }


        // ================================================================
        // Private methods - fragments
        // ================================================================

        private static int Column(ScriptLine line, int token)
        {
            return token < line.Tokens.Count ? line.Tokens[token].Column : line.Raw.Length + 1;
        }

        // "15s", "1.5s" and "800ms" all arrive here; the writer emits seconds for a timeout and
        // milliseconds for a wait, and both read the same way.
        private static int Milliseconds(string text)
        {
            if (text.EndsWith("ms", StringComparison.Ordinal))
                return Integer(text[..^2]);

            if (text.EndsWith('s'))
                return (int)Math.Round(Float(text[..^1]) * 1000f);

            return Integer(text);
        }
    }
}
