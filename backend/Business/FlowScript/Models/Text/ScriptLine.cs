namespace Business.FlowScript.Models.Text
{
    internal sealed class ScriptLine
    {
        public int Number { get; init; }
        public int LeadingSpaces { get; init; }
        public string Raw { get; init; } = string.Empty;
        public IReadOnlyList<ScriptToken> Tokens { get; init; } = [];


        /// <summary>The rest of the line after a prefix it starts with - a comment's text, a stage's name.</summary>
        public string TextAfter(string prefix)
        {
            return Raw.TrimStart()[prefix.Length..].Trim();
        }

        /// <summary>Where a word starts, or just past the end when there is no such word.</summary>
        public int ColumnOf(int index)
        {
            return index < Tokens.Count ? Tokens[index].Column : Raw.Length + 1;
        }

        public string Word(int index)
        {
            return index < Tokens.Count ? Tokens[index].Text : string.Empty;
        }
    }
}
