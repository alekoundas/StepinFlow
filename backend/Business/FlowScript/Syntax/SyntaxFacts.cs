using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;
using Core.Enums;
using Core.Models.Database;

namespace Business.FlowScript.Syntax
{
    /// <summary>
    /// The catalog's words as the printer writes them: the keyword a step is written as, the word for
    /// any value, and the pieces put together from them. The parsers read the same words back.
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

        public static string Condition(FlowStep step)
        {
            if (step.ConditionType == null)
                return string.Empty;

            string keyword = ScriptKeywordCatalog.GetTextOfKeyword(step.ConditionType.Value);

            switch (step.ConditionType)
            {
                case ConditionTypeEnum.IS_EMPTY:
                case ConditionTypeEnum.IS_NOT_EMPTY:
                    return keyword;

                case ConditionTypeEnum.BETWEEN:
                    return $"{keyword} {Quote(step.ConditionText)} {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.AND)} {Quote(step.ConditionTextEnd)}";

                default:
                    return $"{keyword} {Quote(step.ConditionText)}";
            }
        }


        /// <summary>
        /// The button and what it does, left out entirely when it is a plain left click - which is
        /// nearly every click, and saying so on every line would bury the ones that differ.
        /// </summary>
        public static string Button(CursorButtonTypeEnum? button, CursorButtonActionTypeEnum? action)
        {
            string side = string.Empty;
            if (button != null && button != CursorButtonTypeEnum.LEFT_BUTTON)
                side = ScriptKeywordCatalog.GetTextOfKeyword(button.Value);

            string kind = string.Empty;
            if (action != null && action != CursorButtonActionTypeEnum.SINGLE_CLICK)
                kind = ScriptKeywordCatalog.GetTextOfKeyword(action.Value);

            return $"{side} {kind}".Trim();
        }

        /// <summary>
        /// The text quoted: between <c>&lt;[</c> and <c>]&gt;</c>, one space inside each for reading.
        /// Nothing is escaped - double quotes and backslashes are text like anything else.
        /// </summary>
        public static string Quote(string? text)
        {
            return $"{ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.QUOTE_OPEN)} {text} {ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.QUOTE_CLOSE)}";
        }
    }
}
