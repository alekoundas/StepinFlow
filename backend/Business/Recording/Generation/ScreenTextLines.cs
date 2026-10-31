using Business.Recording.Draft;
using Core.Models.Dtos;

namespace Business.Recording.Generation
{
    /// <summary>
    /// The lines of a request that hold text read off the screen, for the tester to read before a
    /// cloud model is sent it: every note, and every line with a name or words the draft took from
    /// what was read on an element - OCR's text, the name Windows had, the words Review searched
    /// by. Found by the words themselves, so a line the tester wrote with the same words is marked
    /// as well: marking too much is the side to err on.
    /// </summary>
    public static class ScreenTextLines
    {
        /// <summary>
        /// Their numbers, from 1. With isReadTextSent false the notes say only that a page began
        /// and what kind of thing was pressed, and are not marked; the words Review searched by
        /// still are, since they go all the same.
        /// </summary>
        public static List<int> Of(string request, RecordingResultDto recording, ReviewedRecordingDto reviewed, bool isReadTextSent)
        {
            List<string> read = ReadTextsOf(recording, reviewed);
            string[] lines = request.Split('\n');
            List<int> marked = [];

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                bool isReadNote = isReadTextSent && ScreenNoteWriter.IsNote(line);
                if (isReadNote || read.Any(x => Holds(line, x)))
                    marked.Add(i + 1);
            }

            return marked;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Whole, as words to search for are written, and cut short, as a name holds them.
        private static List<string> ReadTextsOf(RecordingResultDto recording, ReviewedRecordingDto reviewed)
        {
            IEnumerable<string> texts = recording.ClickedElements.Select(x => x.Text)
                .Concat(recording.PressedElements.Select(x => x.Name))
                .Concat(reviewed.Actions.Select(x => x.SearchText ?? string.Empty));

            return texts
                .SelectMany(x => new[] { RecordingDraft.Quotable(x), RecordingDraft.ShortText(x) })
                .Where(x => x.Any(char.IsLetterOrDigit))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // As whole words: "Go" is not in "Google".
        private static bool Holds(string line, string text)
        {
            int at = line.IndexOf(text, StringComparison.OrdinalIgnoreCase);
            while (at >= 0)
            {
                int end = at + text.Length;
                bool isWordStart = at == 0 || !char.IsLetterOrDigit(line[at - 1]);
                bool isWordEnd = end == line.Length || !char.IsLetterOrDigit(line[end]);
                if (isWordStart && isWordEnd)
                    return true;

                at = line.IndexOf(text, at + 1, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
