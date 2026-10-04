namespace Business.FlowScript.Models.Text
{
    internal sealed class ScriptToken
    {
        public string Value { get; set; } = String.Empty;
        public bool IsQuoted { get; set; }
        public int Column { get; set; }

        /// <summary>Quoted, but its <c>&lt;[</c> never found its <c>]&gt;</c>, so it ran to the end of the line.</summary>
        public bool IsQuoteUnclosed { get; set; }

        public ScriptToken(string value, bool isQuoted, int column)
        {
            Value = value;
            IsQuoted = isQuoted;
            Column = column;
        }
    }
}
