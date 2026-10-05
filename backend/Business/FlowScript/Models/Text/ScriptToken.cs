namespace Business.FlowScript.Models.Text
{
    /// <summary>
    /// One piece of a line, and where it starts. A keyword's value is its catalog text, so the
    /// parser looks it up as is.
    /// </summary>
    internal sealed class ScriptToken
    {
        public ScriptTokenKindEnum Kind { get; }
        public string Value { get; }
        public int Line { get; }
        public int Column { get; }

        public ScriptToken(ScriptTokenKindEnum kind, string value, int line, int column)
        {
            Kind = kind;
            Value = value;
            Line = line;
            Column = column;
        }
    }
}
