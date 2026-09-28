namespace Business.FlowScript.Models
{
    internal sealed class ScriptToken
    {
        public string Text { get; set; } = String.Empty;
        public bool IsQuoted { get; set; }
        public int Column { get; set; }

        public ScriptToken(string text, bool isQuoted, int column)
        {
            Text = text;
            IsQuoted = isQuoted;
            Column = column;
        }
    }
}
