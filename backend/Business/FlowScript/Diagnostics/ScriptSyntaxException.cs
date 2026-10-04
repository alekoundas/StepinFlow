namespace Business.FlowScript.Diagnostics
{
    /// <summary>
    /// A line that stopped making sense, thrown at the token where it did. The Scanner records it
    /// and reads on from the next line.
    /// </summary>
    internal sealed class ScriptSyntaxException : Exception
    {
        public Diagnostic Diagnostic { get; }

        public ScriptSyntaxException(Diagnostic diagnostic) : base(diagnostic.Message)
        {
            Diagnostic = diagnostic;
        }
    }
}
