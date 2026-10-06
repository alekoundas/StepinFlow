using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Diagnostics
{
    /// <summary>
    /// An erroneous line that couldnt be fully read, throws at the current token. 
    /// The Scanner records it and continues to the next line.
    /// </summary>
    internal sealed class ScriptSyntaxException : Exception
    {
        public Diagnostic Diagnostic { get; }

        public ScriptSyntaxException(Diagnostic diagnostic) : base(diagnostic.Message)
        {
            Diagnostic = diagnostic;
        }

        /// <summary>
        /// Any erroneous or out of order token is explained with a user friendly message.
        /// </summary>
        public static ScriptSyntaxException Unexpected(ScriptToken token, IEnumerable<string> expected)
        {
            string found = $"\"{token.Value}\"";
            if (token.Kind == ScriptTokenKindEnum.END_OF_LINE)
                found = "end of the line";

            string message = $"Unexpected {found}.";

            List<string> wanted = expected.Distinct().ToList();
            if (wanted.Count == 1)
                message = $"Unexpected {found}, expected {wanted[0]}.";
            else if (wanted.Count > 1)
                message = $"Unexpected {found}, expected {string.Join(", ", wanted.Take(wanted.Count - 1))} or {wanted[^1]}.";

            return new ScriptSyntaxException(Diagnostic.Error(DiagnosticCodeEnum.TOKEN_UNEXPECTED, token.Line, token.Column, message));
        }
    }
}
