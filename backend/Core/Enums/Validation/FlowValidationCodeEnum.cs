namespace Core.Enums
{
    /// <summary>
    /// What is wrong, as something the UI can branch on rather than a string it has to parse.
    /// The message is for reading; the code is for deciding what to do about it.
    /// </summary>
    public enum FlowValidationCodeEnum
    {
        FLOW_HAS_NO_STEPS,

        // Points
        POINT_MISSING,
        STEP_RESULT_MISSING,

        /// <summary>The step it reads, picked or written as {{name}}, is not above this one on a side that has its result.</summary>
        STEP_RESULT_UNREACHABLE,

        /// <summary>The step it reads keeps nothing: a wait until nothing is found, or {{name}} of a step that keeps no value.</summary>
        STEP_RESULT_NOT_GIVEN,

        // Areas
        AREA_MISSING,

        // Searches
        NO_TEMPLATES,
        SEARCH_TEXT_MISSING,
        OCR_LANGUAGE_MISSING,

        // Conditions
        CONDITION_TYPE_MISSING,
        CONDITION_VALUE_MISSING,
        CONDITION_RANGE_INCOMPLETE,

        // Commands
        COMMAND_MISSING,
        COMMAND_PARAMETER_MISSING,

        // Windows
        WINDOW_SIZE_MISSING,

        /// <summary>A window step's area is a monitor, a region or a browser tab rather than a window.</summary>
        AREA_NOT_A_WINDOW,

        // Control
        LOOP_COUNT_MISSING,

        /// <summary> An End Execution below another one.</summary>
        END_EXECUTION_UNREACHABLE,
        WAIT_RANGE_INVALID,
        SUB_FLOW_MISSING,
        BREAK_TARGET_MISSING,

        /// <summary>Set, but not a step this one sits inside.</summary>
        BREAK_TARGET_UNREACHABLE,

        // Notifications
        DISCORD_BOT_MISSING,

        /// <summary>Set, but the step it reports on no longer fails above this one.</summary>
        FAILED_STEP_UNREACHABLE,

        /// <summary>
        /// Two steps, areas or points share a name. They are one namespace, and the script
        /// references them by name, so a duplicate makes the reference ambiguous.
        /// </summary>
        NAME_DUPLICATE,

        /// <summary>A step writes a name nothing in the flow defines, so it can never resolve.</summary>
        VARIABLE_UNKNOWN,

        /// <summary>Text holding "&lt;[" or "]&gt;", the quotes the flow script writes text between.</summary>
        QUOTE_INSIDE,

        // Warnings
        BRANCHES_EMPTY,
        NAME_MISSING,

        /// <summary>
        /// An area or point in screen coordinates: a region with no parent, or a point measured
        /// from nothing. Right on this screen, and somewhere else on every other.
        /// </summary>
        SCREEN_COORDINATES,

        /// <summary>Keys held with nothing below letting them go, so they stay down until the execution ends.</summary>
        KEYS_NOT_RELEASED,

        // Notes

        /// <summary>A QA test's check with no End Execution failed under its Failure branch, so it never fails the test.</summary>
        CHECK_CANNOT_FAIL,
    }
}
