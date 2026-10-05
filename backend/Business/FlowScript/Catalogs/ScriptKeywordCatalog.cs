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
            new ScriptKeyword("##", FlowStepTypeEnum.MARKER) { TakesRestOfLine = true },

            // The quotes around a name or any other text. Nothing between them is special, so quoted
            // text cannot hold either one.
            new ScriptKeyword("<[", ScriptSymbolEnum.QUOTE_OPEN),
            new ScriptKeyword("]>", ScriptSymbolEnum.QUOTE_CLOSE),

            // Lines that are not steps: intent for the step below, the flow's own fields, and the
            // header opening each section.
            new ScriptKeyword("#", ScriptSymbolEnum.COMMENT) { TakesRestOfLine = true },
            new ScriptKeyword("Flow:", ScriptSymbolEnum.FLOWFIELD_NAME) { TakesRestOfLine = true },
            new ScriptKeyword("Id:", ScriptSymbolEnum.FLOWFIELD_ID) { TakesRestOfLine = true },
            new ScriptKeyword("Sizes:", ScriptSymbolEnum.FLOWFIELD_SIZES),
            new ScriptKeyword("Areas:", ScriptSymbolEnum.AREAS),
            new ScriptKeyword("Points:", ScriptSymbolEnum.POINTS),
            new ScriptKeyword("Inputs:", ScriptSymbolEnum.CSV_COLUMNS),
            new ScriptKeyword("Templates:", ScriptSymbolEnum.TEMPLATES),
            new ScriptKeyword("Steps:", ScriptSymbolEnum.STEPS),

            // FlowArea
            new ScriptKeyword("inside", ScriptSymbolEnum.INSIDE),
            new ScriptKeyword("on screen", ScriptSymbolEnum.ON_SCREEN),
            new ScriptKeyword("ratio", ScriptSymbolEnum.RATIO),
            new ScriptKeyword("offset", ScriptSymbolEnum.OFFSET),
            new ScriptKeyword("at", ScriptSymbolEnum.AT),
            new ScriptKeyword("window", ScriptSymbolEnum.WINDOW),
            new ScriptKeyword("monitor", ScriptSymbolEnum.MONITOR),
            new ScriptKeyword("primary", ScriptSymbolEnum.PRIMARY),
            new ScriptKeyword("process", ScriptSymbolEnum.PROCESS),
            new ScriptKeyword("title", ScriptSymbolEnum.TITLE),
            new ScriptKeyword("size", ScriptSymbolEnum.SIZE),
            new ScriptKeyword("scales with", ScriptSymbolEnum.SCALES_WITH),

            // FlowCsvColumn
            new ScriptKeyword("secret", ScriptSymbolEnum.SECRET),

            // FlowStepTemplate
            new ScriptKeyword("click", ScriptSymbolEnum.CLICK),
            new ScriptKeyword("captured", ScriptSymbolEnum.CAPTURED),

            // FlowStep
            new ScriptKeyword("to", ScriptSymbolEnum.TO),
            new ScriptKeyword("point", ScriptSymbolEnum.POINT),
            new ScriptKeyword("match", ScriptSymbolEnum.MATCH),
            new ScriptKeyword("template", ScriptSymbolEnum.TEMPLATE),
            new ScriptKeyword("accuracy", ScriptSymbolEnum.ACCURACY),
            new ScriptKeyword("required", ScriptSymbolEnum.REQUIRED),
            new ScriptKeyword("in", ScriptSymbolEnum.IN),
            new ScriptKeyword("keep", ScriptSymbolEnum.KEEP),
            new ScriptKeyword("timeout", ScriptSymbolEnum.TIMEOUT),
            new ScriptKeyword("no timeout", ScriptSymbolEnum.NO_TIMEOUT),
            new ScriptKeyword("and", ScriptSymbolEnum.AND),
            new ScriptKeyword("times", ScriptSymbolEnum.TIMES),
            new ScriptKeyword("forever", ScriptSymbolEnum.FOREVER),
            new ScriptKeyword("each", ScriptSymbolEnum.EACH),
            new ScriptKeyword("passed", ScriptSymbolEnum.PASSED),
            new ScriptKeyword("failed", ScriptSymbolEnum.FAILED),

            // Written onto a number: 800ms, 120dpi, 1920x1080.
            new ScriptKeyword("ms", ScriptSymbolEnum.MILLISECONDS),
            new ScriptKeyword("dpi", ScriptSymbolEnum.DPI),
            new ScriptKeyword("x", ScriptSymbolEnum.SIZE_SEPARATOR),

            // What a text or a value is checked against.
            new ScriptKeyword("is not empty", ConditionTypeEnum.IS_NOT_EMPTY),
            new ScriptKeyword("is empty", ConditionTypeEnum.IS_EMPTY),
            new ScriptKeyword("is not", ConditionTypeEnum.NOT_EQUALS),
            new ScriptKeyword("is", ConditionTypeEnum.EQUALS),
            new ScriptKeyword("does not contain", ConditionTypeEnum.NOT_CONTAINS),
            new ScriptKeyword("contains", ConditionTypeEnum.CONTAINS),
            new ScriptKeyword("matches", ConditionTypeEnum.MATCHES_REGEX),
            new ScriptKeyword("between", ConditionTypeEnum.BETWEEN),
            new ScriptKeyword(">", ConditionTypeEnum.GREATER_THAN),
            new ScriptKeyword("<", ConditionTypeEnum.LESS_THAN),

            // A click's button and what it does. Left and single are never written.
            new ScriptKeyword("right", CursorButtonTypeEnum.RIGHT_BUTTON),
            new ScriptKeyword("middle", CursorButtonTypeEnum.MIDDLE_BUTTON),
            new ScriptKeyword("double", CursorButtonActionTypeEnum.DOUBLE_CLICK),
            new ScriptKeyword("hold", CursorButtonActionTypeEnum.HOLD_CLICK),
            new ScriptKeyword("release", CursorButtonActionTypeEnum.RELEASE_CLICK),

            // What System does.
            new ScriptKeyword("LOCK_WORKSTATION", SystemActionTypeEnum.LOCK_WORKSTATION),
            new ScriptKeyword("SLEEP_PC", SystemActionTypeEnum.SLEEP_PC),
            new ScriptKeyword("MONITOR_OFF", SystemActionTypeEnum.MONITOR_OFF),
            new ScriptKeyword("MONITOR_ON", SystemActionTypeEnum.MONITOR_ON),

            // The command Run carries out. Custom and Launch are the steps themselves.
            new ScriptKeyword("KILL_PROCESS", RunCommandPresetEnum.KILL_PROCESS),
            new ScriptKeyword("IS_PROCESS_RUNNING", RunCommandPresetEnum.IS_PROCESS_RUNNING),
            new ScriptKeyword("READ_CLIPBOARD", RunCommandPresetEnum.READ_CLIPBOARD),
            new ScriptKeyword("WRITE_CLIPBOARD", RunCommandPresetEnum.WRITE_CLIPBOARD),
            new ScriptKeyword("CHECK_INTERNET", RunCommandPresetEnum.CHECK_INTERNET),
            new ScriptKeyword("SHUTDOWN_IN", RunCommandPresetEnum.SHUTDOWN_IN),
            new ScriptKeyword("CANCEL_SHUTDOWN", RunCommandPresetEnum.CANCEL_SHUTDOWN),

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
