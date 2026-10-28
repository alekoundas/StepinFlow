namespace Business.FlowScript.Diagnostics
{
    /// <summary>
    /// Whether a file can still be imported. 
    /// An error stops the replace.
    /// a warning is something somebody should look at.
    /// </summary>
    public enum DiagnosticSeverityEnum
    {
        WARNING,
        ERROR,
    }

    public enum DiagnosticCodeEnum
    {
        // Any line: the message and the column say which token and what could have stood there.
        TOKEN_UNEXPECTED,

        // Header: no flow line, or a name that cannot be the file the flow exports to.
        FLOW_LINE_MISSING,
        FLOW_NAME_INVALID,

        // Templates: one a step names with no line above it, one described twice, one not named .png.
        TEMPLATE_UNKNOWN,
        TEMPLATE_DUPLICATE,
        TEMPLATE_NOT_PNG,

        // Steps
        LEADING_SPACES_UNEXPECTED,

        // Comments: one with no step below it.
        COMMENT_UNATTACHED,

        // Names: one used before anything above declares it, one declared twice.
        NAME_UNKNOWN,
        NAME_DUPLICATE,

        // Areas: one inside an area that is already inside another, a second one marked main.
        AREA_TOO_DEEP,
        AREA_MAIN_DUPLICATE,

        // Outside the file: no file at the path, or another flow already has its name.
        FILE_MISSING,
        FLOW_NAME_TAKEN,

        // Sub-flows, found as the file is imported: none by that Id or name, or a flow that is not a sub-flow.
        SUB_FLOW_UNKNOWN,
        SUB_FLOW_NOT_CALLABLE,

        // A model's answer: not the JSON asked for, a flow line or id it changed, a template the
        // draft did not cut, an input it did not rename through the map.
        ANSWER_UNREADABLE,
        FLOW_CHANGED,
        TEMPLATE_NOT_CUT,
        INPUT_NOT_RENAMED,

        // A model's answer for a QA test: no stage marker, no End Execution passed.
        STAGE_MARKER_MISSING,
        END_PASSED_MISSING,
    }

    /// <summary>
    /// Hold the errors and warnings occured during import.
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
