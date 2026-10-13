namespace Core.Helpers
{
    /// <summary>
    /// A command line kept as one string, the way a Launch step stores it: the program first,
    /// quoted when its path has a space, then its arguments.
    /// </summary>
    public static class CommandLineHelper
    {
        /// <summary>The program, without its quotes. What follows it comes back as the arguments, trimmed.</summary>
        public static string SplitProgram(string commandLine, out string arguments)
        {
            string text = commandLine.Trim();

            if (text.StartsWith('"'))
            {
                int close = text.IndexOf('"', 1);
                if (close < 0)
                {
                    arguments = string.Empty;
                    return text.Trim('"');
                }

                arguments = text[(close + 1)..].Trim();
                return text[1..close];
            }

            int space = text.IndexOfAny([' ', '\t']);
            if (space < 0)
            {
                arguments = string.Empty;
                return text;
            }

            arguments = text[(space + 1)..].Trim();
            return text[..space];
        }
    }
}
