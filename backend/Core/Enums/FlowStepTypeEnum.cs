namespace Core.Enums
{
    public enum FlowStepTypeEnum
    {
        // System Steps
        WAIT,
        LOOP,
        GO_TO,
        RUN_CMD,
        SUB_FLOW,
        VARIABLE_CONDITION,
        NOTIFICATION_EMAIL,

        // Input Steps
        CURSOR_DRAG,
        CURSOR_CLICK,
        CURSOR_SCROLL,
        CURSOR_RELOCATE,
        WINDOW_FOCUS,
        WINDOW_RESIZE,
        WINDOW_RELOCATE,
        KYEBOARD_INPUT,

        // Image Search
        IMAGE_LOCATION_EXTRACT,
        TEXT_EXTRACT,



        SUCCESS, // Hidden. Not available for user selection.
        FAILURE, // Hidden. Not available for user selection.
        
        
        //MULTIPLE_TEMPLATE_SEARCH,
        //WAIT_FOR_TEMPLATE,
        //NEW,     // Hidden. Not available for user selection.
        //MULTIPLE_TEMPLATE_SEARCH_CHILD,      // Hidden. Not available for user selection.
    }
}
