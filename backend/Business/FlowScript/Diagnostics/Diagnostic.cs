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
        // Any line: the message and the column say which token and what could have stood there.
        TOKEN_UNEXPECTED,

        // Header
        FLOW_LINE_MISSING,

        // Templates
        TEMPLATE_DUPLICATE,

        // Steps
        INDENT_UNEXPECTED,

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
