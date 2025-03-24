
namespace Model.Enums
{
    public enum FlowStepTypesEnum
    {
        NO_SELECTION,
        WAIT,
        GO_TO,
        LOOP,
        SUB_FLOW_STEP,

        WINDOW_RELOCATE,
        WINDOW_RESIZE,

        CURSOR_SCROLL,
        CURSOR_CLICK,
        CURSOR_RELOCATE,

        TEMPLATE_SEARCH,
        MULTIPLE_TEMPLATE_SEARCH,
        WAIT_FOR_TEMPLATE,

        NEW,     // Hidden. Not available for user selection.
        SUCCESS, // Hidden. Not available for user selection.
        FAILURE, // Hidden. Not available for user selection.
        FLOW_STEPS, // Hidden. Not available for user selection.
        FLOW_PARAMETERS, // Hidden. Not available for user selection.
        MULTIPLE_TEMPLATE_SEARCH_CHILD,      // Hidden. Not available for user selection.
    }
}
