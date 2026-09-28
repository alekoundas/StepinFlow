using Business.FlowScript.Models;
using System.Text;

namespace Business.FlowScript.Syntax
{
    /// <summary>
    /// Text to lines of tokens.
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
            StringBuilder word = new StringBuilder();

            bool inQuotes = false;  // See if we are currently inside quotes
            bool wasQuoted = false; // See if token was a quote
            int quoteIndexStart = 0;// See what possition quote starts

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                // Hanlde quotes.
                if (inQuotes)
                {
                    // The Printer escapes a quote inside a text. ("He said "hello"" -> "He said \"hello\"")
                    if (c == '\\' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        word.Append('"');
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        inQuotes = false;
                        continue;
                    }

                    word.Append(c);
                    continue;
                }

                // Flag inQuote is true.
                if (c == '"')
                {
                    if (word.Length == 0)
                        quoteIndexStart = i;

                    inQuotes = true;
                    wasQuoted = true;
                    continue;
                }

                // Space splits tokens.
                if (char.IsWhiteSpace(c))
                {
                    if (word.Length > 0 || wasQuoted)
                    {
                        tokens.Add(new ScriptToken(word.ToString(), wasQuoted, quoteIndexStart + 1));
                        word.Clear();
                        wasQuoted = false;
                    }

                    continue;
                }

                if (word.Length == 0 && !wasQuoted)
                    quoteIndexStart = i;

                word.Append(c);
            }

            if (word.Length > 0 || wasQuoted)
                tokens.Add(new ScriptToken(word.ToString(), wasQuoted, quoteIndexStart + 1));

            return tokens;
        }
    }
}
