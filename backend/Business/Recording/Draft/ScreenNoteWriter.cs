using Core.Models.Dtos;

namespace Business.Recording.Draft
{
    /// <summary>
    /// What the screen said at a search, as comment lines over it in the draft a model shapes:
    /// whether a new page began there - by its words, or by the window's title - what was on it and
    /// what the local model sees it as, what was pressed as its application named it to Windows or
    /// as the local model sees it, and what appeared and went after the click - for the last, by
    /// the screen the recording stopped on. For the model to name the steps, find the pages and
    /// propose checks by, never for the flow - every line starts with the prefix, and the answer
    /// loses them before it is saved, because they are screen text and the flow is written into a
    /// repository.
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
        private const int MAXIMUM_SENTENCE_LENGTH = 200;

        public static List<string> NotesFor(int actionIndex, RecordingResultDto recording)
        {
            List<string> notes = [];

            ScreenChangeDto? arrived = recording.ScreenChanges.FirstOrDefault(x => x.NextActionIndex == actionIndex);
            if (arrived != null && arrived.IsNewTitle)
                notes.Add(TitleNote(arrived.Title));

            if (arrived != null && arrived.IsNewScreen)
            {
                notes.Add($"{PREFIX} a new page, most of its words changed since the click before");

                string onIt = Listed(arrived.Appeared);
                if (onIt.Length > 0)
                    notes.Add($"{PREFIX} on it {onIt}");
            }

            ScreenDescriptionDto? described = recording.ScreenDescriptions.FirstOrDefault(x => x.ActionIndex == actionIndex);
            if (!string.IsNullOrWhiteSpace(described?.Page))
                notes.Add($"{PREFIX} the local model sees the page as {Quoted(described.Page, MAXIMUM_SENTENCE_LENGTH)}");

            PressedElementDto? pressed = recording.PressedElements.FirstOrDefault(x => x.ActionIndex == actionIndex);
            if (pressed != null)
                notes.Add(PressedNote(pressed));

            if (!string.IsNullOrWhiteSpace(described?.Element))
                notes.Add($"{PREFIX} the local model sees what was pressed as {Quoted(described.Element)}");

            ScreenChangeDto? after = recording.ScreenChanges.FirstOrDefault(x => x.ActionIndex == actionIndex);
            if (after == null)
                return notes;

            // No search follows the screen the recording stopped on to say what is on it.
            if ((after.IsNewScreen || after.IsNewTitle) && after.NextActionIndex == null)
            {
                notes.Add($"{PREFIX} after the click, a new page, where the recording stopped");

                string onIt = Listed(after.Appeared);
                if (onIt.Length > 0)
                    notes.Add($"{PREFIX} on it {onIt}");

                return notes;
            }

            if (after.IsNewScreen || after.IsNewTitle)
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

        // Withheld from a cloud model, the title is gone and only that it changed is said.
        private static string TitleNote(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return $"{PREFIX} the window's title changed";

            return $"{PREFIX} the window's title changed to {Quoted(title)}";
        }

        // As Windows has it from the application: button "Add to cart", not enabled, in group "Shipping".
        private static string PressedNote(PressedElementDto pressed)
        {
            List<string> parts = [pressed.Role];
            if (pressed.Name.Length > 0)
                parts[0] += $" {Quoted(pressed.Name)}";

            if (pressed.IsPassword)
                parts.Add("a password field");

            if (!pressed.IsEnabled)
                parts.Add("not enabled");

            if (pressed.IsChecked == true)
                parts.Add("ticked");

            if (pressed.IsChecked == false)
                parts.Add("not ticked");

            parts.AddRange(pressed.Inside.Select(x => $"in {x}"));

            return $"{PREFIX} what was pressed, as Windows names it: {string.Join(", ", parts)}";
        }

        private static string Listed(IReadOnlyList<string> lines)
        {
            List<string> words = lines
                .Where(x => x.Count(char.IsLetter) >= MINIMUM_LETTERS)
                .Select(x => Quoted(x))
                .ToList();

            string listed = string.Join(", ", words.Take(MAXIMUM_LINES));
            if (words.Count > MAXIMUM_LINES)
                listed += $" and {words.Count - MAXIMUM_LINES} more";

            return listed;
        }

        private static string Quoted(string line)
        {
            return Quoted(line, MAXIMUM_LINE_LENGTH);
        }

        // A quote's brackets cannot stand in a script outside one, and a double quote would end this one.
        private static string Quoted(string line, int maximumLength)
        {
            string text = line.Replace("<[", " ").Replace("]>", " ").Replace('"', '\'').Trim();
            if (text.Length > maximumLength)
                text = text[..maximumLength].TrimEnd() + "…";

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
