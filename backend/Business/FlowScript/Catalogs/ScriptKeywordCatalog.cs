using Business.FlowScript.Models;
using Core.Enums;

namespace Business.FlowScript.Catalogs
{
    /// <summary>
    /// Every word the grammar knows, once. <see cref="Syntax.SyntaxFacts"/> reads it in both
    /// directions - the words a thing is written as, and the thing a set of words means - so nothing
    /// can mean one thing on write and another on read. That is the only way the round trip can be
    /// relied on, and it is why the text lives here and nowhere else.
    ///
    /// A row's enum type is its vocabulary, so the same word can appear in two of them without
    /// ambiguity: "is" and "matches" are both a condition and a way to match a title, and a reader
    /// asking for one never sees the other.
    ///
    /// Longest first within a vocabulary, because reading takes the first match: "Move Window" has to
    /// win over "Move", "Wait Until No Image" over "Wait For Image" over "Wait", "is not empty" over
    /// "is not" over "is", and "shape and brightness" over "shape".
    /// </summary>
    internal static class ScriptKeywordCatalog
    {
        internal static IReadOnlyList<ScriptKeyword> All { get; } =
        [
            // Steps. The keyword says what a check looks at and how at once - "Wait For Image"
            // rather than "Search Image ... wait until found" - which is what makes an impossible
            // combination unwriteable: there is no "Find All Texts" to mistype, because reading an
            // area gives one block of text and nothing to act on each of.
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
    }
}
