namespace Business.FlowScript.Syntax
{
    /// <summary>
    /// One word of a line, and where it started. The column is carried so an error can point at
    /// the word that was wrong rather than at the line that contained it.
    /// </summary>
    public sealed record ScriptToken(string Text, bool WasQuoted, int Column);

    /// <summary>
    /// A line, split into words, with its indentation measured.
    ///
    /// Indent is in levels rather than spaces: the writer indents by two, and a parser that counts
    /// spaces would accept a file no exporter could ever produce.
    /// </summary>
    public sealed class ScriptLine
    {
        public int Number { get; init; }
        public int Indent { get; init; }
        public string Raw { get; init; } = string.Empty;
        public IReadOnlyList<ScriptToken> Tokens { get; init; } = [];

        public bool IsBlank
        {
            get { return Tokens.Count == 0; }
        }

        /// <summary>A section heading, which carries a verdict rather than doing anything.</summary>
        public bool IsSection
        {
            get { return Raw.TrimStart().StartsWith("## ", StringComparison.Ordinal); }
        }

        /// <summary>Intent, attached to the step below it.</summary>
        public bool IsComment
        {
            get
            {
                string trimmed = Raw.TrimStart();

                return trimmed.StartsWith('#') && !trimmed.StartsWith("## ", StringComparison.Ordinal);
            }
        }

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
