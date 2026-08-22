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
        CHECK_VALUE,
        NOTIFY,

        // Input Steps
        CURSOR_DRAG,
        CURSOR_CLICK,
        CURSOR_SCROLL,
        CURSOR_RELOCATE,
        WINDOW_FOCUS,
        WINDOW_RESIZE,
        WINDOW_RELOCATE,
        KEYBOARD_INPUT,

        // Screen Search
        IMAGE_SEARCH,
        READ_TEXT,



        SUCCESS, // Hidden. Not available for user selection.
        FAILURE, // Hidden. Not available for user selection.
        
        
    }
}
