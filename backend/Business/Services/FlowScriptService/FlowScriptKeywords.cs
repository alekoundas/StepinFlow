using Core.Enums;
using Core.Models.Database;

namespace Business.Services.FlowScriptService
{
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
