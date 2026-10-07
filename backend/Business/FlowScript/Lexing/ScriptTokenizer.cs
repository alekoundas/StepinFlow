using Business.FlowScript.Catalogs;
using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Lexing
{
    /// <summary>
    /// A ScriptLine to tokens. It never fails! 
    /// a piece that does not belong is still a token, and the parser reports it where it stands.
    ///
    /// Two passes. The first cuts the line at spaces, keeping quoted text whole.
    /// The second joins neighbouring words into the longest keyword the catalog has.
    /// </summary>
    internal static class ScriptTokenizer
    {
        private static readonly HashSet<string> KeywordTexts = ScriptKeywordCatalog.All.Select(x => x.Text).ToHashSet(StringComparer.Ordinal);

        /// <summary>
        /// Generate tokens by spliting the line on whitespace (except quoted body) and then translate pieces to keywords
        /// </summary>
        public static IReadOnlyList<ScriptToken> Tokenize(ScriptLine line)
        {
            List<ScriptToken> pieces = Cut(line); 
            List<ScriptToken> tokens = Join(pieces); 

            // Add last token.
            tokens.Add(new ScriptToken(ScriptTokenKindEnum.END_OF_LINE, string.Empty, line.Number, line.Raw.Length + 1));

            return tokens;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The quotes and their text, a keyword that takes the rest of the line, and the words between,
        // which leave as UNKNOWN for Join to sort out.
        private static List<ScriptToken> Cut(ScriptLine line)
        {
            string quoteOpen = ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string quoteClose = ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            List<ScriptToken> pieces = new List<ScriptToken>();
            int i = line.LeadingSpaces;

            while (i < line.Raw.Length)
            {
                // Skip whitespace.
                if (char.IsWhiteSpace(line.Raw[i]))
                {
                    i++;
                }

                // A keyword opening with: #, ##, Flow:, Id:
                // Creates 2 tokens (keyword + text)
                else if (pieces.Count == 0 && RestOfLineKeywordAt(line.Raw, i) is ScriptKeyword keyword)
                {
                    pieces.Add(new ScriptToken(ScriptTokenKindEnum.KEYWORD, keyword.Text, line.Number, i + 1));
                    i = SkipWhitespace(line.Raw, i + keyword.Text.Length);

                    pieces.Add(new ScriptToken(ScriptTokenKindEnum.QUOTE, line.Raw[i..], line.Number, i + 1));
                    i = line.Raw.Length;
                }

                // Hanlde quotes(generate 3 tokens): <[ Text ]>
                else if (LineContains(line.Raw, i, quoteOpen))
                {
                    // Add quote <[
                    pieces.Add(new ScriptToken(ScriptTokenKindEnum.KEYWORD, quoteOpen, line.Number, i + 1));
                    i = SkipWhitespace(line.Raw, i + quoteOpen.Length);

                    // Extract text until the quote end ]>, or the end of the line when there is none.
                    int end = line.Raw.IndexOf(quoteClose, i, StringComparison.Ordinal);
                    if (end < 0)
                        end = line.Raw.Length;

                    pieces.Add(new ScriptToken(ScriptTokenKindEnum.QUOTE, line.Raw[i..end].TrimEnd(), line.Number, i + 1));
                    i = end;

                    // Add quote ]>
                    if (end < line.Raw.Length)
                    {
                        pieces.Add(new ScriptToken(ScriptTokenKindEnum.KEYWORD, quoteClose, line.Number, end + 1));
                        i += quoteClose.Length;
                    }
                }

                // A token for all other split by a whitespace.
                else
                {
                    int start = i;
                    while (i < line.Raw.Length && !char.IsWhiteSpace(line.Raw[i]))
                        i++;

                    pieces.Add(new ScriptToken(ScriptTokenKindEnum.UNKNOWN, line.Raw[start..i], line.Number, start + 1));
                }
            }

            return pieces;
        }

        // Combine pieces(UNKNOWN) with the longest possible keywords from catalog.
        private static List<ScriptToken> Join(List<ScriptToken> pieces)
        {
            List<ScriptToken> tokens = new List<ScriptToken>();
            int i = 0;

            while (i < pieces.Count)
            {
                ScriptToken piece = pieces[i];
                if (piece.Kind != ScriptTokenKindEnum.UNKNOWN)
                {
                    tokens.Add(piece);
                    i++;
                    continue;
                }

                // Extract keyword.
                int words = FindLongestKeywordLengthFrom(pieces, i);
                if (words > 0)
                {
                    string keyword = string.Join(" ", pieces.Skip(i).Take(words).Select(x => x.Value));
                    tokens.Add(new ScriptToken(ScriptTokenKindEnum.KEYWORD, keyword, piece.Line, piece.Column));
                    i += words;
                    continue;
                }

                // Extract number.
                // A number starts with a digit or a minus and ends with ms, dpi, x.
                bool isNumber = char.IsDigit(piece.Value[0]) || (piece.Value.Length > 1 && piece.Value[0] == '-' && char.IsDigit(piece.Value[1]));
                if (isNumber)
                    tokens.Add(new ScriptToken(ScriptTokenKindEnum.NUMBER, piece.Value, piece.Line, piece.Column));
                else
                    tokens.Add(piece);

                i++;
            }

            return tokens;
        }

        // How many words from here make the longest keyword, or zero.
        private static int FindLongestKeywordLengthFrom(List<ScriptToken> pieces, int start)
        {
            string phrase = string.Empty;
            int longest = 0;

            for (int i = start; i < pieces.Count && pieces[i].Kind == ScriptTokenKindEnum.UNKNOWN; i++)
            {
                if (i > start) // Skip on first loop.
                    phrase += " ";

                phrase += pieces[i].Value;

                if (KeywordTexts.Contains(phrase))
                    longest = i - start + 1;
            }

            return longest;
        }

        // Find lonest keyword with TakesRestOfLine = true
        private static ScriptKeyword? RestOfLineKeywordAt(string text, int i)
        {
            ScriptKeyword? longest = null;

            foreach (ScriptKeyword keyword in ScriptKeywordCatalog.All.Where(x=>x.TakesRestOfLine))
            {
                if (LineContains(text, i, keyword.Text) && (longest == null || keyword.Text.Length > longest.Text.Length))
                    longest = keyword;
            }

            return longest;
        }

        // The first index from i that is not whitespace, so a text's column is where it starts.
        private static int SkipWhitespace(string text, int i)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;

            return i;
        }

        // Performant way of getting the nec
        private static bool LineContains(string text, int i, string symbol)
        {
            return text.AsSpan(i).StartsWith(symbol, StringComparison.Ordinal);
        }
    }
}
