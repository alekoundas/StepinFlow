using Business.FlowScript.Models.Text;
using Core.Enums;

namespace Business.FlowScript.Catalogs
{
    /// <summary>
    /// Every word the grammar knows, once.
    /// </summary>
    internal static class ScriptKeywordCatalog
    {
        public static IReadOnlyList<ScriptKeyword> All { get; } =
        [
            // Steps.
            new ScriptKeyword("Wait Until No Image", FlowStepTypeEnum.SEARCH_IMAGE, SearchModeEnum.WAIT_UNTIL_NOT_FOUND),
            new ScriptKeyword("Wait Until No Text", FlowStepTypeEnum.SEARCH_TEXT, SearchModeEnum.WAIT_UNTIL_NOT_FOUND),
            new ScriptKeyword("Find All Images", FlowStepTypeEnum.SEARCH_IMAGE, SearchModeEnum.FIND_ALL),
            new ScriptKeyword("Wait For Image", FlowStepTypeEnum.SEARCH_IMAGE, SearchModeEnum.WAIT_UNTIL_FOUND),
            new ScriptKeyword("Wait For Text", FlowStepTypeEnum.SEARCH_TEXT, SearchModeEnum.WAIT_UNTIL_FOUND),
            new ScriptKeyword("End Execution", FlowStepTypeEnum.END_EXECUTION),
            new ScriptKeyword("Resize Window", FlowStepTypeEnum.WINDOW_RESIZE),
            new ScriptKeyword("Focus Window", FlowStepTypeEnum.WINDOW_FOCUS),
            new ScriptKeyword("Move Window", FlowStepTypeEnum.WINDOW_RELOCATE),
            new ScriptKeyword("Check Value", FlowStepTypeEnum.CHECK_VALUE),
            new ScriptKeyword("Find Image", FlowStepTypeEnum.SEARCH_IMAGE, SearchModeEnum.FIND_BEST),
            new ScriptKeyword("Check Text", FlowStepTypeEnum.SEARCH_TEXT, SearchModeEnum.FIND_BEST),
            new ScriptKeyword("Sub Flow", FlowStepTypeEnum.SUB_FLOW),
            new ScriptKeyword("Go To", FlowStepTypeEnum.GO_TO),
            new ScriptKeyword("Notify", FlowStepTypeEnum.NOTIFY),
            new ScriptKeyword("Scroll", FlowStepTypeEnum.CURSOR_SCROLL),
            new ScriptKeyword("System", FlowStepTypeEnum.SYSTEM_ACTION),
            new ScriptKeyword("Launch", FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPresetEnum.LAUNCH_APP),
            new ScriptKeyword("Click", FlowStepTypeEnum.CURSOR_CLICK),
            new ScriptKeyword("Press", FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputTypeEnum.COMBINATION),
            new ScriptKeyword("Drag", FlowStepTypeEnum.CURSOR_DRAG),
            new ScriptKeyword("Move", FlowStepTypeEnum.CURSOR_RELOCATE),
            new ScriptKeyword("Loop", FlowStepTypeEnum.LOOP),
            new ScriptKeyword("Type", FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputTypeEnum.TEXT),
            new ScriptKeyword("Wait", FlowStepTypeEnum.WAIT),
            new ScriptKeyword("Run", FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPresetEnum.CUSTOM),

            // The rows the tree carries: a check's two branches, and a stage heading.
            new ScriptKeyword("Success:", FlowStepTypeEnum.SUCCESS),
            new ScriptKeyword("Failure:", FlowStepTypeEnum.FAILURE),
            new ScriptKeyword("##", FlowStepTypeEnum.MARKER),

            // The quotes around a name or any other text. Nothing between them is special, so quoted
            // text cannot hold either one.
            new ScriptKeyword("<[", ScriptSymbolEnum.QUOTE_OPEN),
            new ScriptKeyword("]>", ScriptSymbolEnum.QUOTE_CLOSE),

            // Lines that are not steps: intent for the step below, the flow's own fields, and the
            // header opening each section.
            new ScriptKeyword("#", ScriptSymbolEnum.COMMENT),
            new ScriptKeyword("Flow:", ScriptSymbolEnum.FLOW),
            new ScriptKeyword("Id:", ScriptSymbolEnum.ID),
            new ScriptKeyword("Sizes:", ScriptSymbolEnum.SIZES),
            new ScriptKeyword("Areas:", ScriptSymbolEnum.AREAS),
            new ScriptKeyword("Points:", ScriptSymbolEnum.POINTS),
            new ScriptKeyword("Inputs:", ScriptSymbolEnum.INPUTS),
            new ScriptKeyword("Templates:", ScriptSymbolEnum.TEMPLATES),
            new ScriptKeyword("Steps:", ScriptSymbolEnum.STEPS),

            // How a window title is matched. "is" and "matches" are also conditions; the vocabulary
            // is the enum type, so a reader asking for one never finds the other.
            new ScriptKeyword("starts with", TitleMatchModeEnum.STARTS_WITH),
            new ScriptKeyword("contains", TitleMatchModeEnum.CONTAINS),
            new ScriptKeyword("matches", TitleMatchModeEnum.REGEX),
            new ScriptKeyword("is", TitleMatchModeEnum.EQUALS),

            // How templates are compared.
            new ScriptKeyword("shape and brightness", TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS),
            new ScriptKeyword("shape", TemplateMatchModeEnum.SHAPE),

            // What an area's contents follow.
            new ScriptKeyword("dpi", ScalesWithEnum.DPI),
            new ScriptKeyword("area", ScalesWithEnum.AREA),

            // Which way a scroll goes.
            new ScriptKeyword("up", CursorScrollDirectionTypeEnum.UP),
            new ScriptKeyword("down", CursorScrollDirectionTypeEnum.DOWN),
            new ScriptKeyword("left", CursorScrollDirectionTypeEnum.LEFT),
            new ScriptKeyword("right", CursorScrollDirectionTypeEnum.RIGHT),
        ];

        /// <summary>
        /// TEnum picks the vocabulary, so a word shared by two of them never finds the wrong one.
        /// </summary>
        public static ScriptKeyword? Get<TEnum>(string text) where TEnum : struct, Enum
        {
            foreach (ScriptKeyword keyword in All)
            {
                if (keyword.Type is TEnum && string.Equals(keyword.Text, text, StringComparison.Ordinal))
                    return keyword;
            }

            return null;
        }

        /// <summary>
        /// The first row of this type, whatever its modifier.
        /// </summary>
        public static ScriptKeyword? Get(Enum type)
        {
            foreach (ScriptKeyword keyword in All)
            {
                if (keyword.Type.Equals(type))
                    return keyword;
            }

            return null;
        }

        public static ScriptKeyword? Get(Enum type, Enum modifier)
        {
            foreach (ScriptKeyword keyword in All)
            {
                if (keyword.Type.Equals(type) && modifier.Equals(keyword.Modifier))
                    return keyword;
            }

            return null;
        }
    }
}
