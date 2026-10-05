using Business.FlowScript.Catalogs;
using Business.FlowScript.Lexing;
using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Syntax
{
    /// <summary>
    /// The lines and the words on them, for the parsers that read words. Goes when they do.
    /// </summary>
    public static class ScriptWordReader
    {
        internal static IReadOnlyList<ScriptLine> Read(string script)
        {
            IReadOnlyList<ScriptLine> lines = ScriptLineSplitter.Split(script);

            foreach (ScriptLine line in lines)
                line.Words = Tokenize(line.Raw);

            return lines;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static IReadOnlyList<ScriptWord> Tokenize(string line)
        {
            string quoteOpen = SyntaxFacts.Keyword(ScriptSymbolEnum.QUOTE_OPEN);

            List<ScriptWord> tokens = new List<ScriptWord>();
            int i = 0;

            while (i < line.Length)
            {
                if (char.IsWhiteSpace(line[i]))
                {
                    i++;
                    continue;
                }

                int column = i + 1;
                if (line.AsSpan(i).StartsWith(quoteOpen, StringComparison.Ordinal))
                    tokens.Add(ReadQuoted(line, ref i, column));
                else
                    tokens.Add(new ScriptWord(ReadUnquoted(line, ref i), false, column));
            }

            return tokens;
        }

        // From "<[" to the first "]>", trimmed. Nothing between the quotes is special, so a quoted value
        // cannot hold "]>" - and one that never closes runs to the end of the line, for the parser to report.
        private static ScriptWord ReadQuoted(string line, ref int i, int column)
        {
            string quoteOpen = SyntaxFacts.Keyword(ScriptSymbolEnum.QUOTE_OPEN);
            string quoteClose = SyntaxFacts.Keyword(ScriptSymbolEnum.QUOTE_CLOSE);

            int from = i + quoteOpen.Length;
            int to = line.IndexOf(quoteClose, from, StringComparison.Ordinal);

            if (to < 0)
            {
                i = line.Length;
                return new ScriptWord(line[from..].Trim(), true, column) { IsQuoteUnclosed = true };
            }

            i = to + quoteClose.Length;
            return new ScriptWord(line[from..to].Trim(), true, column);
        }

        // Up to the next whitespace.
        private static string ReadUnquoted(string line, ref int i)
        {
            int start = i;
            while (i < line.Length && !char.IsWhiteSpace(line[i]))
                i++;

            return line[start..i];
        }

        //private static IReadOnlyList<ScriptWord> Tokenize(string line)
        //{
        //    List<ScriptWord> tokens = new List<ScriptWord>();
        //    StringBuilder word = new StringBuilder();

        //    bool inQuotes = false;  // See if we are currently inside quotes
        //    bool wasQuoted = false; // See if token was a quote
        //    int tokenIndexStart = 0;// See what possition quote starts

        //    for (int i = 0; i < line.Length; i++)
        //    {
        //        char c = line[i];

        //        // Hanlde quotes.
        //        if (inQuotes)
        //        {
        //            // The Printer escapes a quote inside a text. ("He said "hello"" -> "He said \"hello\"")
        //            if (c == '\\' && i + 1 < line.Length && line[i + 1] == '"') // if this and next char are: \"
        //            {
        //                word.Append('"');
        //                i++;
        //                continue;
        //            }

        //            if (c == '"')
        //            {
        //                inQuotes = false;
        //                continue;
        //            }

        //            word.Append(c);
        //            continue;
        //        }

        //        // Flag inQuote is true.
        //        if (c == '"')
        //        {
        //            if (word.Length == 0)
        //                tokenIndexStart = i;

        //            inQuotes = true;
        //            wasQuoted = true;
        //            continue;
        //        }

        //        // Space splits tokens.
        //        if (char.IsWhiteSpace(c))
        //        {
        //            if (word.Length > 0 || wasQuoted)
        //            {
        //                tokens.Add(new ScriptWord(word.ToString(), wasQuoted, tokenIndexStart + 1));
        //                word.Clear();
        //                wasQuoted = false;
        //            }

        //            continue;
        //        }

        //        if (word.Length == 0 && !wasQuoted)
        //            tokenIndexStart = i;

        //        word.Append(c);
        //    }

        //    if (word.Length > 0 || wasQuoted)
        //        tokens.Add(new ScriptWord(word.ToString(), wasQuoted, tokenIndexStart + 1));

        //    return tokens;
        //}
    }
}
