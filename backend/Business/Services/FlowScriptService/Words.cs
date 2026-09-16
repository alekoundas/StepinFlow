using Core.Enums;
using Core.Models.Database;

namespace Business.Services.FlowScriptService
{
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

        private static string Quoted(string? text)
        {
            return "\"" + (text ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }
    }
}
