using System.Globalization;

using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Syntax
{
    /// <summary>A condition read back off a line, and how many words it took.</summary>
    public sealed record ConditionSyntax(ConditionTypeEnum Type, string Text, string TextEnd, int Words);

    /// <summary>A keyword and everything about a step that the keyword alone decides.</summary>
    public sealed record ScriptKeyword(
        string Text,
        FlowStepTypeEnum Type,
        SearchModeEnum? SearchMode = null,
        KeyboardInputTypeEnum? KeyboardInputType = null,
        RunCommandPresetEnum? RunCommandPreset = null);

    /// <summary>
    /// Everything the grammar knows about a word, in both directions.
    ///
    /// The keyword a step is written as and the step a keyword means; the words for a condition,
    /// a title match, a scroll direction and a mouse button, and those words read back. One file
    /// because they are one table: a keyword that meant one thing on write and another on read is
    /// a round trip that cannot be relied on, and two files is how that happens.
    ///
    /// The word a step is written as.
    ///
    /// A check's keyword says what it looks at and how it looks at once - "Wait For Image" rather
    /// than "Search Image ... wait until found". That is what makes an impossible combination
    /// unwriteable: there is no "Find All Texts" to mistype, because reading an area gives one
    /// block of text and nothing to act on each of.
    /// </summary>
    public static class SyntaxFacts
    {
        public static string For(FlowStep step)
        {
            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.SEARCH_IMAGE:
                    return ImageKeyword(step.SearchMode);

                case FlowStepTypeEnum.SEARCH_TEXT:
                    return TextKeyword(step.SearchMode);

                case FlowStepTypeEnum.CHECK_VALUE:
                    return "Check Value";

                case FlowStepTypeEnum.CURSOR_CLICK:
                    return "Click";

                case FlowStepTypeEnum.CURSOR_RELOCATE:
                    return "Move";

                case FlowStepTypeEnum.CURSOR_DRAG:
                    return "Drag";

                case FlowStepTypeEnum.CURSOR_SCROLL:
                    return "Scroll";

                case FlowStepTypeEnum.KEYBOARD_INPUT:
                    return step.KeyboardInputType == KeyboardInputTypeEnum.COMBINATION ? "Press" : "Type";

                case FlowStepTypeEnum.WAIT:
                    return "Wait";

                case FlowStepTypeEnum.LOOP:
                    return "Loop";

                case FlowStepTypeEnum.GO_TO:
                    return "Go To";

                case FlowStepTypeEnum.SUB_FLOW:
                    return "Sub Flow";

                case FlowStepTypeEnum.NOTIFY:
                    return "Notify";

                case FlowStepTypeEnum.END_EXECUTION:
                    return "End Execution";

                case FlowStepTypeEnum.SYSTEM_ACTION:
                    return "System";

                case FlowStepTypeEnum.WINDOW_FOCUS:
                    return "Focus Window";

                case FlowStepTypeEnum.WINDOW_RESIZE:
                    return "Resize Window";

                case FlowStepTypeEnum.WINDOW_RELOCATE:
                    return "Move Window";

                // A launch is a command with a preset, and reads as what it does rather than as the
                // machinery underneath.
                case FlowStepTypeEnum.SYSTEM_COMMAND:
                    return step.RunCommandPreset == RunCommandPresetEnum.LAUNCH_APP ? "Launch" : "Run";

                default:
                    return step.FlowStepType.ToString();
            }
        }


        /// <summary>
        /// The same table read the other way. One list, so a keyword cannot mean one thing on write
        /// and another on read - which is the only way a round trip can be relied on.
        ///
        /// Longest first: "Move Window" has to win over "Move", and "Wait Until No Image" over
        /// "Wait For Image" over "Wait".
        /// </summary>
        public static IReadOnlyList<ScriptKeyword> All { get; } =
        [
            new ScriptKeyword("Wait Until No Image", FlowStepTypeEnum.SEARCH_IMAGE, SearchMode: SearchModeEnum.WAIT_UNTIL_NOT_FOUND),
            new ScriptKeyword("Wait Until No Text", FlowStepTypeEnum.SEARCH_TEXT, SearchMode: SearchModeEnum.WAIT_UNTIL_NOT_FOUND),
            new ScriptKeyword("Find All Images", FlowStepTypeEnum.SEARCH_IMAGE, SearchMode: SearchModeEnum.FIND_ALL),
            new ScriptKeyword("Wait For Image", FlowStepTypeEnum.SEARCH_IMAGE, SearchMode: SearchModeEnum.WAIT_UNTIL_FOUND),
            new ScriptKeyword("Wait For Text", FlowStepTypeEnum.SEARCH_TEXT, SearchMode: SearchModeEnum.WAIT_UNTIL_FOUND),
            new ScriptKeyword("End Execution", FlowStepTypeEnum.END_EXECUTION),
            new ScriptKeyword("Resize Window", FlowStepTypeEnum.WINDOW_RESIZE),
            new ScriptKeyword("Focus Window", FlowStepTypeEnum.WINDOW_FOCUS),
            new ScriptKeyword("Move Window", FlowStepTypeEnum.WINDOW_RELOCATE),
            new ScriptKeyword("Check Value", FlowStepTypeEnum.CHECK_VALUE),
            new ScriptKeyword("Find Image", FlowStepTypeEnum.SEARCH_IMAGE, SearchMode: SearchModeEnum.FIND_BEST),
            new ScriptKeyword("Check Text", FlowStepTypeEnum.SEARCH_TEXT, SearchMode: SearchModeEnum.FIND_BEST),
            new ScriptKeyword("Sub Flow", FlowStepTypeEnum.SUB_FLOW),
            new ScriptKeyword("Go To", FlowStepTypeEnum.GO_TO),
            new ScriptKeyword("Notify", FlowStepTypeEnum.NOTIFY),
            new ScriptKeyword("Scroll", FlowStepTypeEnum.CURSOR_SCROLL),
            new ScriptKeyword("System", FlowStepTypeEnum.SYSTEM_ACTION),
            new ScriptKeyword("Launch", FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset: RunCommandPresetEnum.LAUNCH_APP),
            new ScriptKeyword("Click", FlowStepTypeEnum.CURSOR_CLICK),
            new ScriptKeyword("Press", FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType: KeyboardInputTypeEnum.COMBINATION),
            new ScriptKeyword("Drag", FlowStepTypeEnum.CURSOR_DRAG),
            new ScriptKeyword("Move", FlowStepTypeEnum.CURSOR_RELOCATE),
            new ScriptKeyword("Loop", FlowStepTypeEnum.LOOP),
            new ScriptKeyword("Type", FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType: KeyboardInputTypeEnum.TEXT),
            new ScriptKeyword("Wait", FlowStepTypeEnum.WAIT),
            new ScriptKeyword("Run", FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset: RunCommandPresetEnum.CUSTOM),
        ];

        /// <summary>
        /// The keyword a line starts with, and how many words it took. Null when the first word is
        /// not a keyword at all, which is what the reader reports as an unknown step.
        /// </summary>
        public static ScriptKeyword? Match(IReadOnlyList<ScriptToken> tokens)
        {
            foreach (ScriptKeyword keyword in All)
            {
                string[] words = keyword.Text.Split(' ');
                if (tokens.Count < words.Length)
                    continue;

                bool matched = true;
                for (int i = 0; i < words.Length; i++)
                {
                    // A quoted word is a name that happens to read like a keyword, not a keyword.
                    if (tokens[i].WasQuoted || !string.Equals(tokens[i].Text, words[i], StringComparison.Ordinal))
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                    return keyword;
            }

            return null;
        }

        // ================================================================
        // Private methods
        // ================================================================

        private static string ImageKeyword(SearchModeEnum mode)
        {
            switch (mode)
            {
                case SearchModeEnum.FIND_ALL:
                    return "Find All Images";

                case SearchModeEnum.WAIT_UNTIL_FOUND:
                    return "Wait For Image";

                case SearchModeEnum.WAIT_UNTIL_NOT_FOUND:
                    return "Wait Until No Image";

                default:
                    return "Find Image";
            }
        }

        private static string TextKeyword(SearchModeEnum mode)
        {
            switch (mode)
            {
                case SearchModeEnum.WAIT_UNTIL_FOUND:
                    return "Wait For Text";

                case SearchModeEnum.WAIT_UNTIL_NOT_FOUND:
                    return "Wait Until No Text";

                // FIND_ALL is not offered for text, so anything that is not a wait reads as a check.
                default:
                    return "Check Text";
            }
        }
        public static string Condition(FlowStep step)
        {
            string value = Quoted(step.ConditionText);

            switch (step.ConditionType)
            {
                case ConditionTypeEnum.EQUALS: return $"is {value}";
                case ConditionTypeEnum.NOT_EQUALS: return $"is not {value}";
                case ConditionTypeEnum.CONTAINS: return $"contains {value}";
                case ConditionTypeEnum.NOT_CONTAINS: return $"does not contain {value}";
                case ConditionTypeEnum.MATCHES_REGEX: return $"matches {value}";
                case ConditionTypeEnum.IS_EMPTY: return "is empty";
                case ConditionTypeEnum.IS_NOT_EMPTY: return "is not empty";
                case ConditionTypeEnum.GREATER_THAN: return $"> {value}";
                case ConditionTypeEnum.LESS_THAN: return $"< {value}";
                case ConditionTypeEnum.BETWEEN: return $"between {value} and {Quoted(step.ConditionTextEnd)}";
                default: return string.Empty;
            }
        }

        public static string TitleMatch(TitleMatchModeEnum mode)
        {
            switch (mode)
            {
                case TitleMatchModeEnum.EQUALS: return "is";
                case TitleMatchModeEnum.STARTS_WITH: return "starts with";
                case TitleMatchModeEnum.REGEX: return "matches";
                default: return "contains";
            }
        }

        public static string ScrollDirection(CursorScrollDirectionTypeEnum? direction)
        {
            switch (direction)
            {
                case CursorScrollDirectionTypeEnum.UP: return "up";
                case CursorScrollDirectionTypeEnum.LEFT: return "left";
                case CursorScrollDirectionTypeEnum.RIGHT: return "right";
                default: return "down";
            }
        }

        /// <summary>
        /// The button and what it does, left out entirely when it is a plain left click - which is
        /// nearly every click, and saying so on every line would bury the ones that differ.
        /// </summary>
        public static string Button(CursorButtonTypeEnum? button, CursorButtonActionTypeEnum? action)
        {
            string side = button switch
            {
                CursorButtonTypeEnum.RIGHT_BUTTON => "right",
                CursorButtonTypeEnum.MIDDLE_BUTTON => "middle",
                _ => string.Empty,
            };

            string kind = action switch
            {
                CursorButtonActionTypeEnum.DOUBLE_CLICK => "double",
                CursorButtonActionTypeEnum.HOLD_CLICK => "hold",
                CursorButtonActionTypeEnum.RELEASE_CLICK => "release",
                _ => string.Empty,
            };

            return $"{side} {kind}".Trim();
        }

        // ================================================================
        // Public methods - the same tables, read backwards
        // ================================================================

        /// <summary>
        /// A condition from the words it was written as. Longest first again: "is not empty" has
        /// to beat "is not", which has to beat "is".
        /// </summary>
        public static ConditionSyntax? ReadCondition(IReadOnlyList<ScriptToken> tokens, int at)
        {
            string W(int i)
            {
                return at + i < tokens.Count && !tokens[at + i].WasQuoted ? tokens[at + i].Text : string.Empty;
            }

            string V(int i)
            {
                return at + i < tokens.Count ? tokens[at + i].Text : string.Empty;
            }

            if (W(0) == "is" && W(1) == "empty")
                return new ConditionSyntax(ConditionTypeEnum.IS_EMPTY, string.Empty, string.Empty, 2);

            if (W(0) == "is" && W(1) == "not" && W(2) == "empty")
                return new ConditionSyntax(ConditionTypeEnum.IS_NOT_EMPTY, string.Empty, string.Empty, 3);

            if (W(0) == "is" && W(1) == "not")
                return new ConditionSyntax(ConditionTypeEnum.NOT_EQUALS, V(2), string.Empty, 3);

            if (W(0) == "is")
                return new ConditionSyntax(ConditionTypeEnum.EQUALS, V(1), string.Empty, 2);

            if (W(0) == "does" && W(1) == "not" && W(2) == "contain")
                return new ConditionSyntax(ConditionTypeEnum.NOT_CONTAINS, V(3), string.Empty, 4);

            if (W(0) == "contains")
                return new ConditionSyntax(ConditionTypeEnum.CONTAINS, V(1), string.Empty, 2);

            if (W(0) == "matches")
                return new ConditionSyntax(ConditionTypeEnum.MATCHES_REGEX, V(1), string.Empty, 2);

            if (W(0) == "between" && W(2) == "and")
                return new ConditionSyntax(ConditionTypeEnum.BETWEEN, V(1), V(3), 4);

            if (W(0) == ">")
                return new ConditionSyntax(ConditionTypeEnum.GREATER_THAN, V(1), string.Empty, 2);

            if (W(0) == "<")
                return new ConditionSyntax(ConditionTypeEnum.LESS_THAN, V(1), string.Empty, 2);

            return null;
        }

        /// <summary>How a window title is matched, and how many words that took.</summary>
        public static (TitleMatchModeEnum Mode, int Words)? ReadTitleMatch(IReadOnlyList<ScriptToken> tokens, int at)
        {
            string first = at < tokens.Count ? tokens[at].Text : string.Empty;
            string second = at + 1 < tokens.Count ? tokens[at + 1].Text : string.Empty;

            if (first == "starts" && second == "with")
                return (TitleMatchModeEnum.STARTS_WITH, 2);

            switch (first)
            {
                case "is": return (TitleMatchModeEnum.EQUALS, 1);
                case "matches": return (TitleMatchModeEnum.REGEX, 1);
                case "contains": return (TitleMatchModeEnum.CONTAINS, 1);
                default: return null;
            }
        }

        public static CursorScrollDirectionTypeEnum? ReadScrollDirection(string word)
        {
            switch (word)
            {
                case "up": return CursorScrollDirectionTypeEnum.UP;
                case "down": return CursorScrollDirectionTypeEnum.DOWN;
                case "left": return CursorScrollDirectionTypeEnum.LEFT;
                case "right": return CursorScrollDirectionTypeEnum.RIGHT;
                default: return null;
            }
        }

        /// <summary>
        /// The button and what it does. Both halves are optional and either order is unambiguous,
        /// because "right" is never an action and "double" is never a side.
        /// </summary>
        public static (CursorButtonTypeEnum Button, CursorButtonActionTypeEnum Action) ReadButton(IEnumerable<string> words)
        {
            CursorButtonTypeEnum button = CursorButtonTypeEnum.LEFT_BUTTON;
            CursorButtonActionTypeEnum action = CursorButtonActionTypeEnum.SINGLE_CLICK;

            foreach (string word in words)
            {
                switch (word)
                {
                    case "right": button = CursorButtonTypeEnum.RIGHT_BUTTON; break;
                    case "middle": button = CursorButtonTypeEnum.MIDDLE_BUTTON; break;
                    case "double": action = CursorButtonActionTypeEnum.DOUBLE_CLICK; break;
                    case "hold": action = CursorButtonActionTypeEnum.HOLD_CLICK; break;
                    case "release": action = CursorButtonActionTypeEnum.RELEASE_CLICK; break;
                    default: break;
                }
            }

            return (button, action);
        }

        private static string Quoted(string? text)
        {
            return "\"" + (text ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        // ================================================================
        // Public methods - how the grammar writes a number
        // ================================================================

        /// <summary>Invariant, because the file is machine written and machine read.</summary>
        public static float Float(string text)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;
        }

        /// <inheritdoc cref="Float"/>
        public static int Integer(string text)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
        }

        /// <summary>
        /// A duration. The printer writes seconds for a timeout and milliseconds for a wait, so
        /// "15s", "1.5s" and "800ms" all arrive here and all read the same way.
        /// </summary>
        public static int Milliseconds(string text)
        {
            if (text.EndsWith("ms", StringComparison.Ordinal))
                return Integer(text[..^2]);

            if (text.EndsWith('s'))
                return (int)Math.Round(Float(text[..^1]) * 1000f);

            return Integer(text);
        }

    }
}
