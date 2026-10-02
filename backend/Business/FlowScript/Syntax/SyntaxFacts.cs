using System.Globalization;
using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Syntax
{
    /// <summary>A condition read back off a line, and how many words it took.</summary>
    public sealed record ConditionSyntax(ConditionTypeEnum Type, string Text, string TextEnd, int Words);

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
        /// <summary>
        /// The keyword a step is written as. A step with none is a gap in the catalog, so it throws
        /// rather than writing a line no reader accepts.
        /// </summary>
        public static string For(FlowStep step)
        {
            ScriptKeyword? keyword;
            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.SEARCH_IMAGE:
                case FlowStepTypeEnum.SEARCH_TEXT:
                    keyword = ScriptKeywordCatalog.Get(step.FlowStepType, step.SearchMode);
                    break;

                case FlowStepTypeEnum.KEYBOARD_INPUT:
                    KeyboardInputTypeEnum type = KeyboardInputTypeEnum.TEXT;
                    if (step.KeyboardInputType == KeyboardInputTypeEnum.COMBINATION)
                        type = KeyboardInputTypeEnum.COMBINATION;

                    keyword = ScriptKeywordCatalog.Get(step.FlowStepType, type);
                    break;

                case FlowStepTypeEnum.SYSTEM_COMMAND:
                    RunCommandPresetEnum commandType = RunCommandPresetEnum.CUSTOM;
                    if (step.RunCommandPreset == RunCommandPresetEnum.LAUNCH_APP)
                        commandType = RunCommandPresetEnum.LAUNCH_APP;

                    keyword = ScriptKeywordCatalog.Get(step.FlowStepType, commandType);
                    break;

                default:
                    keyword = ScriptKeywordCatalog.Get(step.FlowStepType);
                    break;
            }
            if (keyword == null)
                throw new InvalidOperationException($"No keyword in the catalog writes this {step.FlowStepType} step.");

            return keyword.Text;
        }


        /// <summary>
        /// The step keyword a line starts with. Null when it starts with none, which the reader
        /// reports as an unknown step.
        /// </summary>
        internal static ScriptKeyword? ReadFirstKeyword(IReadOnlyList<ScriptToken> tokens)
        {
            return ReadKeyword<FlowStepTypeEnum>(tokens, 0);
        }

        /// <summary>A "#" line is intent for the step below it, unless the catalog says otherwise - "##" is a stage heading.</summary>
        internal static bool IsComment(ScriptLine line)
        {
            if (!line.Raw.TrimStart().StartsWith(LineType(ScriptLineTypeEnum.COMMENT), StringComparison.Ordinal))
                return false;

            return ReadFirstKeyword(line.Tokens) == null;
        }

        /// <summary>The section a header line opens. Null when the line is not a header.</summary>
        internal static ScriptLineTypeEnum? ReadSectionHeader(ScriptLine line)
        {
            ScriptLineTypeEnum? type = ReadLineType(line.Raw.Trim());
            switch (type)
            {
                case ScriptLineTypeEnum.AREAS:
                case ScriptLineTypeEnum.POINTS:
                case ScriptLineTypeEnum.INPUTS:
                case ScriptLineTypeEnum.TEMPLATES:
                case ScriptLineTypeEnum.STEPS:
                    return type;

                default:
                    return null;
            }
        }

        internal static ScriptLineTypeEnum? ReadLineType(string text)
        {
            return ScriptKeywordCatalog.Get<ScriptLineTypeEnum>(text)?.As<ScriptLineTypeEnum>();
        }

        internal static string LineType(ScriptLineTypeEnum type)
        {
            return ScriptKeywordCatalog.Get(type)!.Text;
        }

        /// <summary>How a window title is matched.</summary>
        internal static ScriptKeyword? ReadTitleMatch(IReadOnlyList<ScriptToken> tokens, int tokenIndex)
        {
            return ReadKeyword<TitleMatchModeEnum>(tokens, tokenIndex);
        }

        /// <summary>How templates are compared.</summary>
        internal static ScriptKeyword? ReadMatchMode(IReadOnlyList<ScriptToken> tokens, int tokenIndex)
        {
            return ReadKeyword<TemplateMatchModeEnum>(tokens, tokenIndex);
        }

        public static ScalesWithEnum? ReadScalesWith(string word)
        {
            return ScriptKeywordCatalog.Get<ScalesWithEnum>(word)?.As<ScalesWithEnum>();
        }

        public static CursorScrollDirectionTypeEnum? ReadScrollDirection(string word)
        {
            return ScriptKeywordCatalog.Get<CursorScrollDirectionTypeEnum>(word)?.As<CursorScrollDirectionTypeEnum>();
        }

        public static string TitleMatch(TitleMatchModeEnum mode)
        {
            return ScriptKeywordCatalog.Get(mode)?.Text ?? string.Empty;
        }

        public static string ScalesWith(ScalesWithEnum scalesWith)
        {
            return ScriptKeywordCatalog.Get(scalesWith)?.Text ?? string.Empty;
        }

        public static string MatchMode(TemplateMatchModeEnum mode)
        {
            return ScriptKeywordCatalog.Get(mode)?.Text ?? string.Empty;
        }

        /// <summary>A scroll with no direction goes down, which is what it did before there was one.</summary>
        public static string ScrollDirection(CursorScrollDirectionTypeEnum? direction)
        {
            return ScriptKeywordCatalog.Get(direction ?? CursorScrollDirectionTypeEnum.DOWN)?.Text ?? string.Empty;
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


        /// <summary>
        /// A condition from the words it was written as. Longest first again: "is not empty" has
        /// to beat "is not", which has to beat "is".
        /// </summary>
        internal static ConditionSyntax? ReadCondition(IReadOnlyList<ScriptToken> tokens, int at)
        {
            string W(int i)
            {
                if (at + i >= tokens.Count || tokens[at + i].IsQuoted)
                    return string.Empty;

                return tokens[at + i].Text;
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

        /// <summary>
        /// A member of <typeparamref name="TEnum"/> by its exact name. Not Enum.TryParse, which
        /// also takes a number - any number, whether a member has it or not.
        /// </summary>
        public static bool TryReadName<TEnum>(string word, out TEnum value) where TEnum : struct, Enum
        {
            value = default;

            if (!Enum.GetNames<TEnum>().Contains(word, StringComparer.Ordinal))
                return false;

            value = Enum.Parse<TEnum>(word);
            return true;
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

        /// <summary>"120dpi", or null when that is not what it says.</summary>
        public static int? ReadDpi(string text)
        {
            if (!text.EndsWith("dpi", StringComparison.Ordinal))
                return null;

            if (!int.TryParse(text[..^3], NumberStyles.None, CultureInfo.InvariantCulture, out int dpi) || dpi <= 0)
                return null;

            return dpi;
        }

        /// <summary>Two integers joined by one character - "120,40" for a click, "800x600" for a size.</summary>
        public static (int First, int Second)? ReadPair(string text, char separator)
        {
            string[] parts = text.Split(separator);
            if (parts.Length != 2)
                return null;

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int first)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int second))
                return null;

            return (first, second);
        }

        // ================================================================
        // Private methods
        // ================================================================

        // Find the longest keyword the tokens can generate starting from "tokenIndex".
        // ex "Wait Until No Image" = 4 tokens but 1 command.
        private static ScriptKeyword? ReadKeyword<TEnum>(IReadOnlyList<ScriptToken> tokens, int tokenIndex) where TEnum : struct, Enum
        {
            ScriptKeyword? longest = null;
            string phrase = string.Empty;

            for (int i = tokenIndex; i < tokens.Count && !tokens[i].IsQuoted; i++)
            {
                if (i > tokenIndex) // Dont add space on first loop.
                    phrase += " ";

                phrase += tokens[i].Text;

                ScriptKeyword? keyword = ScriptKeywordCatalog.Get<TEnum>(phrase);
                if (keyword != null)
                    longest = keyword;
            }

            return longest;
        }


        private static string Quoted(string? text)
        {
            return "\"" + (text ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

      
    }
}
