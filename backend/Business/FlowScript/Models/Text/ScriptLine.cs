namespace Business.FlowScript.Models.Text
{
    internal sealed class ScriptLine
    {
        public int Number { get; set; }
        public int LeadingSpaces { get; set; }
        public string Raw { get; set; } = string.Empty;
        public IReadOnlyList<ScriptToken> Tokens { get; set; } = [];
    }
}
