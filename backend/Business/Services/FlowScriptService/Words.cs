using Core.Enums;
using Core.Models.Database;

namespace Business.Services.FlowScriptService
{
    /// <summary>A condition read back off a line, and how many words it took.</summary>
    public sealed record ParsedCondition(ConditionTypeEnum Type, string Text, string TextEnd, int Words);

    /// <summary>
    /// Enum members as the words the script uses.
    ///
    /// `CONTAINS` is a database value; `contains` is what somebody reviewing a pull request reads.
    /// Kept apart from the writer so the parser can read the same table backwards.
    /// </summary>
    public static class Words
    {
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
        public static ParsedCondition? ReadCondition(IReadOnlyList<ScriptToken> tokens, int at)
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
                return new ParsedCondition(ConditionTypeEnum.IS_EMPTY, string.Empty, string.Empty, 2);

            if (W(0) == "is" && W(1) == "not" && W(2) == "empty")
                return new ParsedCondition(ConditionTypeEnum.IS_NOT_EMPTY, string.Empty, string.Empty, 3);

            if (W(0) == "is" && W(1) == "not")
                return new ParsedCondition(ConditionTypeEnum.NOT_EQUALS, V(2), string.Empty, 3);

            if (W(0) == "is")
                return new ParsedCondition(ConditionTypeEnum.EQUALS, V(1), string.Empty, 2);

            if (W(0) == "does" && W(1) == "not" && W(2) == "contain")
                return new ParsedCondition(ConditionTypeEnum.NOT_CONTAINS, V(3), string.Empty, 4);

            if (W(0) == "contains")
                return new ParsedCondition(ConditionTypeEnum.CONTAINS, V(1), string.Empty, 2);

            if (W(0) == "matches")
                return new ParsedCondition(ConditionTypeEnum.MATCHES_REGEX, V(1), string.Empty, 2);

            if (W(0) == "between" && W(2) == "and")
                return new ParsedCondition(ConditionTypeEnum.BETWEEN, V(1), V(3), 4);

            if (W(0) == ">")
                return new ParsedCondition(ConditionTypeEnum.GREATER_THAN, V(1), string.Empty, 2);

            if (W(0) == "<")
                return new ParsedCondition(ConditionTypeEnum.LESS_THAN, V(1), string.Empty, 2);

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
    }
}
