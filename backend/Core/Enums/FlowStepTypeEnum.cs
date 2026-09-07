namespace Core.Enums
{
    public enum FlowStepTypeEnum
    {
        // System Steps
        WAIT,
        LOOP,
        GO_TO,
        SYSTEM_COMMAND,
        SYSTEM_ACTION,
        SUB_FLOW,
        NOTIFY,
        END_EXECUTION,
        MARKER,

        // Input Steps
        CURSOR_DRAG,
        CURSOR_CLICK,
        CURSOR_SCROLL,
        CURSOR_RELOCATE,
        WINDOW_FOCUS,
        WINDOW_RESIZE,
        WINDOW_RELOCATE,
        KEYBOARD_INPUT,

        // Screen Reading
        SEARCH_IMAGE,
        SEARCH_TEXT,

        // Decisions.
        CHECK_VALUE,

        SUCCESS, // Hidden. Not available for user selection.
        FAILURE, // Hidden. Not available for user selection.
    }
}
