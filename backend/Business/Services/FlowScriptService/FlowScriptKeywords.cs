using Core.Enums;
using Core.Models.Database;

namespace Business.Services.FlowScriptService
{
    /// <summary>A keyword and everything about a step that the keyword alone decides.</summary>
    public sealed record ScriptKeyword(
        string Text,
        FlowStepTypeEnum Type,
        SearchModeEnum? SearchMode = null,
        KeyboardInputTypeEnum? KeyboardInputType = null,
        RunCommandPresetEnum? RunCommandPreset = null);

    /// <summary>
    /// The word a step is written as.
    ///
    /// A check's keyword says what it looks at and how it looks at once - "Wait For Image" rather
    /// than "Search Image ... wait until found". That is what makes an impossible combination
    /// unwriteable: there is no "Find All Texts" to mistype, because reading an area gives one
    /// block of text and nothing to act on each of.
    /// </summary>
    public static class FlowScriptKeywords
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
    }
}
