using System.Text;

namespace Business.Services.FlowScriptService
{
    /// <summary>
    /// Text to lines of words.
    ///
    /// The one rule that makes this simple is the writer's: every name is quoted, always, even when
    /// it would read fine without. So a quoted run is a single word whatever is inside it, and
    /// nothing else needs escaping.
    /// </summary>
    public static class ScriptTokenizer
    {
        private const int IndentWidth = 2;

        public static IReadOnlyList<ScriptLine> Read(string script)
        {
            List<ScriptLine> lines = new List<ScriptLine>();
            string[] raw = (script ?? string.Empty).ReplaceLineEndings("\n").Split('\n');

            for (int i = 0; i < raw.Length; i++)
            {
                string text = raw[i].TrimEnd();

                lines.Add(new ScriptLine
                {
                    Number = i + 1,
                    Indent = IndentOf(text),
                    Raw = text,
                    Tokens = Tokenize(text),
                });
            }

            return lines;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Tabs are not indentation here. The writer never emits one, so a file containing them was
        // hand-edited, and guessing a tab width is how two readers disagree about a tree.
        private static int IndentOf(string text)
        {
            int spaces = 0;
            while (spaces < text.Length && text[spaces] == ' ')
                spaces++;

            return spaces / IndentWidth;
        }

        private static IReadOnlyList<ScriptToken> Tokenize(string text)
        {
            List<ScriptToken> tokens = new List<ScriptToken>();
            StringBuilder word = new StringBuilder();

            bool inQuotes = false;
            bool wasQuoted = false;
            int start = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    // The writer escapes a quote inside a name, and nothing else.
                    if (c == '\\' && i + 1 < text.Length && text[i + 1] == '"')
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
                        tokens.Add(new ScriptToken(word.ToString(), wasQuoted, start + 1));
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
                tokens.Add(new ScriptToken(word.ToString(), wasQuoted, start + 1));

            return tokens;
        }
    }
}
