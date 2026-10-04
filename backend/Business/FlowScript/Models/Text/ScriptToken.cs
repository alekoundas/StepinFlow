namespace Business.FlowScript.Models.Text
{
    internal sealed class ScriptToken
    {
        public string Text { get; set; } = String.Empty;
        public bool IsQuoted { get; set; }
        public int Column { get; set; }

        /// <summary>Text whose <c>&lt;[</c> never found its <c>]&gt;</c>, so it ran to the end of the line.</summary>
        public bool IsUnclosed { get; set; }

        public ScriptToken(string text, bool isQuoted, int column)
        {
            Text = text;
            IsQuoted = isQuoted;
            Column = column;
        }
    }
}
