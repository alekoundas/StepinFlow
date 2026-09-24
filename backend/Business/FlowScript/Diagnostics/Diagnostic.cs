namespace Business.FlowScript.Diagnostics
{
    /// <summary>
    /// Whether a file can still be imported. An error stops the replace; a warning is something
    /// the flow can live with and somebody should look at.
    /// </summary>
    public enum DiagnosticSeverityEnum
    {
        WARNING,
        ERROR,
    }

    /// <summary>
    /// What went wrong, as something to branch on rather than a string to match.
    ///
    /// The same idea as FlowValidationCodeEnum next door: the message is for reading and the code
    /// is for deciding what to do about it.
    /// </summary>
    public enum DiagnosticCodeEnum
    {
        // Header
        FLOW_LINE_MISSING,
        HEADER_MALFORMED,
        HEADER_UNKNOWN,
        ID_MALFORMED,
        SIZE_MALFORMED,

        // Areas, points and inputs
        AREA_NAME_MISSING,
        AREA_WINDOW_MALFORMED,
        AREA_PLACEMENT_UNKNOWN,
        PLACEMENT_MALFORMED,
        POINT_NAME_MISSING,
        POINT_PLACEMENT_UNKNOWN,
        INPUT_MALFORMED,
        TITLE_MATCH_UNKNOWN,

        // Steps
        STEP_UNKNOWN,
        CONDITION_MISSING,
        SEARCH_ARGUMENT_UNKNOWN,
        TARGET_MISSING,
        DRAG_TARGET_MISSING,
        SCROLL_DIRECTION_UNKNOWN,
        DURATION_MALFORMED,
        LOOP_MALFORMED,
        SYSTEM_ACTION_UNKNOWN,
        PROCESS_MISSING,
        ACCURACY_WITHOUT_TEMPLATE,

        // Binding
        NAME_UNKNOWN,

        // Outside the file
        FILE_MISSING,
    }

    /// <summary>
    /// Where a file stopped making sense, what was expected instead, and whether that stops the
    /// import.
    ///
    /// No length yet: a span would let an editor underline the word rather than point at the line,
    /// and there is no script editor to do that with. Worth adding with the editor, not before.
    /// </summary>
    public sealed record Diagnostic(DiagnosticCodeEnum Code, DiagnosticSeverityEnum Severity, int Line, int Column, string Message)
    {
        public static Diagnostic Error(DiagnosticCodeEnum code, int line, int column, string message)
        {
            return new Diagnostic(code, DiagnosticSeverityEnum.ERROR, line, column, message);
        }

        public static Diagnostic Warning(DiagnosticCodeEnum code, int line, int column, string message)
        {
            return new Diagnostic(code, DiagnosticSeverityEnum.WARNING, line, column, message);
        }
    }
}
