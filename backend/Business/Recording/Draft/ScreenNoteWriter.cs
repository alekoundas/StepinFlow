using Core.Models.Dtos;

namespace Business.Recording.Draft
{
    /// <summary>
    /// What the screen said at a search, as comment lines over it in the draft a model shapes:
    /// whether a new page began there and what was on it, and what appeared and went after the
    /// click. For the model to name the steps and find the pages by, never for the flow - every
    /// line starts with the prefix, and the answer loses them before it is saved, because they are
    /// screen text and the flow is written into a repository.
    ///
    /// Only lines of four letters or more, and a handful of those: a game's counters and a misread
    /// icon are not words. A long line is cut short with an ellipsis.
    /// </summary>
    public static class ScreenNoteWriter
    {
        public const string PREFIX = "screen:";

        private const int MINIMUM_LETTERS = 4;
        private const int MAXIMUM_LINES = 6;
        private const int MAXIMUM_LINE_LENGTH = 50;

        public static List<string> NotesFor(int actionIndex, IReadOnlyList<ScreenChangeDto> changes)
        {
            List<string> notes = [];

            ScreenChangeDto? arrived = changes.FirstOrDefault(x => x.NextActionIndex == actionIndex);
            if (arrived != null && arrived.IsNewScreen)
            {
                notes.Add($"{PREFIX} a new page, most of its words changed since the click before");

                string onIt = Listed(arrived.Appeared);
                if (onIt.Length > 0)
                    notes.Add($"{PREFIX} on it {onIt}");
            }

            ScreenChangeDto? after = changes.FirstOrDefault(x => x.ActionIndex == actionIndex);
            if (after == null)
                return notes;

            if (after.IsNewScreen)
            {
                notes.Add($"{PREFIX} after the click, a new page");
                return notes;
            }

            string appeared = Listed(after.Appeared);
            if (appeared.Length > 0)
                notes.Add($"{PREFIX} after the click appeared {appeared}");

            string gone = Listed(after.Gone);
            if (gone.Length > 0)
                notes.Add($"{PREFIX} after the click went {gone}");

            return notes;
        }

        /// <summary>The script without a single note, whatever the model did with them.</summary>
        public static string WithoutNotes(string script)
        {
            IEnumerable<string> lines = script.Split('\n').Where(x => !IsNote(x));
            return string.Join('\n', lines);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string Listed(IReadOnlyList<string> lines)
        {
            List<string> words = lines
                .Where(x => x.Count(char.IsLetter) >= MINIMUM_LETTERS)
                .Select(Quoted)
                .ToList();

            string listed = string.Join(", ", words.Take(MAXIMUM_LINES));
            if (words.Count > MAXIMUM_LINES)
                listed += $" and {words.Count - MAXIMUM_LINES} more";

            return listed;
        }

        // A quote's brackets cannot stand in a script outside one, and a double quote would end this one.
        private static string Quoted(string line)
        {
            string text = line.Replace("<[", " ").Replace("]>", " ").Replace('"', '\'').Trim();
            if (text.Length > MAXIMUM_LINE_LENGTH)
                text = text[..MAXIMUM_LINE_LENGTH].TrimEnd() + "…";

            return $"\"{text}\"";
        }

        // A comment line that starts with the prefix, however the model spaced or cased it. A
        // stage marker's ## is not one.
        private static bool IsNote(string line)
        {
            string text = line.TrimStart();
            if (!text.StartsWith('#'))
                return false;

            return text[1..].TrimStart().StartsWith(PREFIX, StringComparison.OrdinalIgnoreCase);
        }
    }
}
