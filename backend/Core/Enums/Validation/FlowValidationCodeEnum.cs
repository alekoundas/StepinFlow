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

        /// <summary>Set, but the step it reads no longer runs above this one through Success.</summary>
        STEP_RESULT_UNREACHABLE,

        // Areas
        AREA_MISSING,

        // Searches
        NO_TEMPLATES,
        SEARCH_TEXT_MISSING,
        OCR_LANGUAGE_MISSING,

        // Commands
        COMMAND_MISSING,
        COMMAND_PARAMETER_MISSING,

        // Windows
        WINDOW_SIZE_MISSING,

        // Control
        LOOP_COUNT_MISSING,
        SUB_FLOW_MISSING,

        // Warnings
        BRANCHES_EMPTY,
        NAME_MISSING,
    }
}
