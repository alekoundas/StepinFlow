using Business.FlowScript.Models.Text;
using System.Text;

namespace Business.FlowScript.Syntax
{
    /// <summary>
    /// Read the script and convert to lines and tokens.
    /// </summary>
    public static class ScriptTokenizer
    {
        internal static IReadOnlyList<ScriptLine> Read(string script)
        {
            List<ScriptLine> lines = new List<ScriptLine>();

            // Splits natively on Windows, Linux, or Mac line endings in a single pass
            string[] raw = script.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

            for (int i = 0; i < raw.Length; i++)
            {
                string line = raw[i].TrimEnd();

                lines.Add(new ScriptLine
                {
                    Number = i + 1,
                    LeadingSpaces = LeadingSpacesOf(line),
                    Raw = line,
                    Tokens = Tokenize(line),
                });
            }

            return lines;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static int LeadingSpacesOf(string line)
        {
            int spacesCount = 0;

            foreach (char c in line)
            {
                if (c == ' ')
                    spacesCount += 1;

                else if (c == '\t')
                    spacesCount += 1; // Tab width always 1.

                else
                    break; // Break on the very first actual character
            }

            return spacesCount;
        }

        private static IReadOnlyList<ScriptToken> Tokenize(string line)
        {
            List<ScriptToken> tokens = new List<ScriptToken>();
            int i = 0;

            while (i < line.Length)
            {
                if (char.IsWhiteSpace(line[i]))
                {
                    i++;
                    continue;
                }

                int column = i + 1;
                if (line[i] == '"')
                    tokens.Add(new ScriptToken(ReadQuoted(line, ref i), true, column));
                else
                    tokens.Add(new ScriptToken(ReadWord(line, ref i), false, column));
            }

            return tokens;
        }

        // From an opening quote to its closing one. Backslashes are plain text unless they come right
        // before a quote: there each pair is one backslash, and an odd one left over makes the quote
        // part of the text rather than its end. SyntaxFacts.Quote writes them that way.
        private static string ReadQuoted(string line, ref int i)
        {
            StringBuilder text = new StringBuilder();
            i++;

            while (i < line.Length && line[i] != '"')
            {
                if (line[i] != '\\')
                {
                    text.Append(line[i]);
                    i++;
                    continue;
                }

                int runEnd = i;
                while (runEnd < line.Length && line[runEnd] == '\\')
                    runEnd++;

                int backslashes = runEnd - i;
                i = runEnd;

                bool beforeQuote = i < line.Length && line[i] == '"';
                if (!beforeQuote)
                {
                    text.Append('\\', backslashes);
                    continue;
                }

                text.Append('\\', backslashes / 2);

                // Odd: the quote is text. Even: it closes the text, which the loop sees next.
                if (backslashes % 2 == 1)
                {
                    text.Append('"');
                    i++;
                }
            }

            i++;
            return text.ToString();
        }

        // Up to the next whitespace.
        private static string ReadWord(string line, ref int i)
        {
            int start = i;
            while (i < line.Length && !char.IsWhiteSpace(line[i]))
                i++;

            return line[start..i];
        }

        //private static IReadOnlyList<ScriptToken> Tokenize(string line)
        //{
        //    List<ScriptToken> tokens = new List<ScriptToken>();
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
        //                tokens.Add(new ScriptToken(word.ToString(), wasQuoted, tokenIndexStart + 1));
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
        //        tokens.Add(new ScriptToken(word.ToString(), wasQuoted, tokenIndexStart + 1));

        //    return tokens;
        //}
    }
}
