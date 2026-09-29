using System.Globalization;
using Business.FlowScript.Catalogs;
using Business.FlowScript.Models;
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
        /// The words a step is written as, taken from the catalogue rather than spelled out again -
        /// a second copy of the text is how a keyword comes to mean one thing on write and another
        /// on read.
        ///
        /// A type with no keyword at all - SUCCESS, FAILURE, MARKER, which the tree carries rather
        /// than the grammar - falls back to its own name, which no reader accepts. That is deliberate:
        /// such a step is never printed as a line of its own.
        /// </summary>
        public static string For(FlowStep step)
        {
            return Keyword(step)?.Text ?? step.FlowStepType.ToString();
        }


        /// <summary>
        /// The keyword a line starts with, and how many words it took. Null when the first word is
        /// not a keyword at all, which is what the reader reports as an unknown step.
        /// </summary>
        internal static ScriptKeyword? Match(IReadOnlyList<ScriptToken> tokens)
        {
            return ReadWord<FlowStepTypeEnum>(tokens, 0);
        }

        /// <summary>How a window title is matched, and how many words that took.</summary>
        internal static (TitleMatchModeEnum Mode, int Words)? ReadTitleMatch(IReadOnlyList<ScriptToken> tokens, int at)
        {
            return Read<TitleMatchModeEnum>(tokens, at);
        }

        /// <summary>How templates are compared, and how many words that took. Longest first again.</summary>
        internal static (TemplateMatchModeEnum Mode, int Words)? ReadMatchMode(IReadOnlyList<ScriptToken> tokens, int at)
        {
            return Read<TemplateMatchModeEnum>(tokens, at);
        }

        public static ScalesWithEnum? ReadScalesWith(string word)
        {
            return Member<ScalesWithEnum>(word);
        }

        public static CursorScrollDirectionTypeEnum? ReadScrollDirection(string word)
        {
            return Member<CursorScrollDirectionTypeEnum>(word);
        }

        public static string TitleMatch(TitleMatchModeEnum mode)
        {
            return Word(mode);
        }

        public static string ScalesWith(ScalesWithEnum scalesWith)
        {
            return Word(scalesWith);
        }

        public static string MatchMode(TemplateMatchModeEnum mode)
        {
            return Word(mode);
        }

        /// <summary>A scroll with no direction goes down, which is what it did before there was one.</summary>
        public static string ScrollDirection(CursorScrollDirectionTypeEnum? direction)
        {
            return Word(direction ?? CursorScrollDirectionTypeEnum.DOWN);
        }

        // ================================================================
        // Private methods
        // ================================================================

        // The four below are the whole of reading and writing a one-to-one vocabulary, and they are
        // generic because the row's enum type is what says which vocabulary it belongs to. TEnum is
        // the filter, so "is" as a title match and "is" as a condition never reach each other.

        // The words a member is written as.
        private static string Word<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            foreach (ScriptKeyword keyword in ScriptKeywordCatalog.All)
            {
                if (keyword.TypeIs(value))
                    return keyword.Text;
            }

            return string.Empty;
        }

        // The member a single word means, or null when that vocabulary has no such word.
        private static TEnum? Member<TEnum>(string word) where TEnum : struct, Enum
        {
            foreach (ScriptKeyword keyword in ScriptKeywordCatalog.All)
            {
                if (keyword.Type is TEnum && string.Equals(keyword.Text, word, StringComparison.Ordinal))
                    return keyword.As<TEnum>();
            }

            return null;
        }

        // The member the tokens from "at" spell out, with how many words it took. The count comes from
        // the phrase, so a two-word form cannot disagree with the number its reader returns.
        private static (TEnum Mode, int Words)? Read<TEnum>(IReadOnlyList<ScriptToken> tokens, int at) where TEnum : struct, Enum
        {
            ScriptKeyword? keyword = ReadWord<TEnum>(tokens, at);
            if (keyword == null)
                return null;

            return (keyword.As<TEnum>()!.Value, keyword.Text.Split(' ').Length);
        }

        private static ScriptKeyword? ReadWord<TEnum>(IReadOnlyList<ScriptToken> tokens, int at) where TEnum : struct, Enum
        {
            foreach (ScriptKeyword keyword in ScriptKeywordCatalog.All)
            {
                if (keyword.Type is TEnum && LeadsWith(tokens, at, keyword.Text))
                    return keyword;
            }

            return null;
        }

        // Whether the tokens from "at" are exactly these words, unquoted. A quoted word is a name
        // that happens to read like a keyword, never the keyword itself.
        private static bool LeadsWith(IReadOnlyList<ScriptToken> tokens, int at, string text)
        {
            string[] words = text.Split(' ');
            if (at + words.Length > tokens.Count)
                return false;

            for (int i = 0; i < words.Length; i++)
            {
                if (tokens[at + i].IsQuoted || !string.Equals(tokens[at + i].Text, words[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        // Which row of the catalogue a step is written as. Two types cannot be found by matching
        // their discriminator, and both read as what they do rather than as the machinery underneath:
        // a keyboard step is Type unless it sends a combination, and a command is Run unless its
        // preset is a launch - Run covers every other preset, so no row names them.
        private static ScriptKeyword? Keyword(FlowStep step)
        {
            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.SEARCH_IMAGE:
                case FlowStepTypeEnum.SEARCH_TEXT:
                    // A mode with no keyword of its own reads as the plain search. FIND_ALL is not
                    // offered for text, so a text step in that mode is a Check Text.
                    return WithModifier(step.FlowStepType, step.SearchMode)
                        ?? WithModifier(step.FlowStepType, SearchModeEnum.FIND_BEST);

                case FlowStepTypeEnum.KEYBOARD_INPUT:
                    KeyboardInputTypeEnum typed = KeyboardInputTypeEnum.TEXT;
                    if (step.KeyboardInputType == KeyboardInputTypeEnum.COMBINATION)
                        typed = KeyboardInputTypeEnum.COMBINATION;

                    return WithModifier(step.FlowStepType, typed);

                case FlowStepTypeEnum.SYSTEM_COMMAND:
                    RunCommandPresetEnum preset = RunCommandPresetEnum.CUSTOM;
                    if (step.RunCommandPreset == RunCommandPresetEnum.LAUNCH_APP)
                        preset = RunCommandPresetEnum.LAUNCH_APP;

                    return WithModifier(step.FlowStepType, preset);

                default:
                    foreach (ScriptKeyword keyword in ScriptKeywordCatalog.All)
                    {
                        if (keyword.TypeIs(step.FlowStepType))
                            return keyword;
                    }

                    return null;
            }
        }

        // A row whose type is this step's and whose modifier is that member. Equals, never ==: both
        // slots are typed Enum, so == would compare the boxes rather than the members.
        private static ScriptKeyword? WithModifier<TEnum>(FlowStepTypeEnum type, TEnum modifier) where TEnum : struct, Enum
        {
            foreach (ScriptKeyword keyword in ScriptKeywordCatalog.All)
            {
                if (keyword.TypeIs(type) && keyword.Modifier != null && keyword.Modifier.Equals(modifier))
                    return keyword;
            }

            return null;
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

        // ================================================================
        // Public methods - the same tables, read backwards
        // ================================================================

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
    }
}
