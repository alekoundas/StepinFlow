using Business.FlowScript.Models;
using System.Text;

namespace Business.FlowScript.Syntax
{
    /// <summary>
    /// Text to lines of words.
    /// </summary>
    public static class Lexer
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
                    spacesCount += 4; // Tab width.

                else
                    break; // Break on the very first actual character
            }

            return spacesCount;
        }

        private static IReadOnlyList<ScriptLineToken> Tokenize(string line)
        {
            List<ScriptLineToken> tokens = new List<ScriptLineToken>();
            StringBuilder word = new StringBuilder();

            bool inQuotes = false;
            bool wasQuoted = false;
            int start = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    // The writer escapes a quote inside a name, and nothing else.
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

                if (c == '"')
                {
                    if (word.Length == 0)
                        start = i;

                    inQuotes = true;
                    wasQuoted = true;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    if (word.Length > 0 || wasQuoted)
                    {
                        tokens.Add(new ScriptLineToken(word.ToString(), wasQuoted, start + 1));
                        word.Clear();
                        wasQuoted = false;
                    }

                    continue;
                }

                if (word.Length == 0 && !wasQuoted)
                    start = i;

                word.Append(c);
            }

            if (word.Length > 0 || wasQuoted)
                tokens.Add(new ScriptLineToken(word.ToString(), wasQuoted, start + 1));

            return tokens;
        }
    }
}
