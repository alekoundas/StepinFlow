namespace Business.FlowScript.Models
{
    internal sealed class ScriptToken
    {
        public string Text { get; set; } = String.Empty;
        public bool WasQuoted { get; set; }
        public int Column { get; set; }

        public ScriptToken(string text, bool wasQuoted, int column)
        {
            Text = text;
            WasQuoted = wasQuoted;
            Column = column;
        }
    }
}
