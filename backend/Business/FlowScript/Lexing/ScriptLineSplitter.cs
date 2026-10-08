using Business.FlowScript.Models.Text;

namespace Business.FlowScript.Lexing
{
    /// <summary>
    /// The script as numbered lines, each with its leading spaces.
    /// </summary>
    internal static class ScriptLineSplitter
    {
        public static IReadOnlyList<ScriptLine> Split(string script)
        {
            List<ScriptLine> lines = new List<ScriptLine>();

            // Windows, Linux and Mac line endings in one pass.
            string[] raw = script.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

            for (int i = 0; i < raw.Length; i++)
            {
                string text = raw[i].TrimEnd();

                lines.Add(new ScriptLine()
                {
                    Number = i + 1,
                    LeadingSpaces = LeadingSpacesOf(text),
                    Raw = text,
                });
            }

            return lines;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // One level is one space, and a tab counts as one.
        private static int LeadingSpacesOf(string text)
        {
            int count = 0;
            while (count < text.Length && (text[count] == ' ' || text[count] == '\t'))
                count++;

            return count;
        }
    }
}
