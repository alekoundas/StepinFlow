namespace Business.FlowScript.Models.Text
{
    internal sealed class ScriptLine
    {
        public int Number { get; set; }
        public int LeadingSpaces { get; set; }
        public string Raw { get; set; } = string.Empty;
        public IReadOnlyList<ScriptToken> Tokens { get; set; } = [];


        /// <summary>The rest of the raw line after a prefix it starts with - a comment, a stage's name.</summary>
        public string RawAfter(string prefix)
        {
            return Raw.TrimStart()[prefix.Length..].Trim();
        }

        /// <summary>Where a token starts, or just past the end when there is no such token.</summary>
        public int ColumnOf(int index)
        {
            return index < Tokens.Count ? Tokens[index].Column : Raw.Length + 1;
        }

        /// <summary>What a token holds, quoted or not, or empty when there is no such token.</summary>
        public string ValueAt(int index)
        {
            return index < Tokens.Count ? Tokens[index].Value : string.Empty;
        }
    }
}
