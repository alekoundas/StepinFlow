namespace Business.FlowScript.Models.Text
{
    internal sealed class ScriptLine
    {
        public int Number { get; init; }
        public int LeadingSpaces { get; init; }
        public string Raw { get; init; } = string.Empty;
        public IReadOnlyList<ScriptToken> Tokens { get; init; } = [];


        public string TextAfterHash
        {
            get { return Raw.TrimStart().TrimStart('#').Trim(); }
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
