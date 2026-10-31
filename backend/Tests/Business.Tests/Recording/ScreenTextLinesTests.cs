using Business.Recording.Draft;
using Business.Recording.Generation;
using Core.Models.Dtos;

namespace Business.Tests.Recording
{
    /// <summary>
    /// Which lines of a request hold text read off the screen: every note, and every line with a
    /// name or words read on an element, as whole words.
    /// </summary>
    public sealed class ScreenTextLinesTests
    {
        private readonly RecordingResultDto _recording = new RecordingResultDto();
        private readonly ReviewedRecordingDto _reviewed = new ReviewedRecordingDto();

        private List<int> Marked(params string[] lines)
        {
            return ScreenTextLines.Of(string.Join('\n', lines), _recording, _reviewed, true);
        }

        [Fact]
        public void Every_note_is_marked_and_no_other_comment()
        {
            List<int> marked = Marked(
                "Flow: Download invoices",
                "    # screen: a new page, most of its words changed since the click before",
                "    # recorded after a 1.2s wait",
                "## Sign in");

            marked.ShouldBe([2]);
        }

        [Fact]
        public void A_line_with_the_words_read_on_an_element_is_marked_as_whole_words_only()
        {
            _recording.ClickedElements.Add(new ClickedElementDto { ActionIndex = 0, Text = "Go" });

            List<int> marked = Marked(
                "Search Image <[ Find Go ]>",
                "Description: Opens Google and searches.");

            marked.ShouldBe([1]);
        }

        // A name holds the words cut short; what is searched for holds them whole.
        [Fact]
        public void Words_too_long_for_a_name_are_marked_cut_short_and_whole()
        {
            string words = "Accept all cookies and continue to the site you came for";
            _reviewed.Actions.Add(new ReviewedActionDto { ActionIndex = 0, SearchText = words });

            List<int> marked = Marked(
                $"Search Text <[ {RecordingDraft.NameFrom("Find", words, "Find icon")} ]>",
                $"    Contains <[ {words} ]>",
                "End Execution passed");

            marked.ShouldBe([1, 2]);
        }

        // An icon's name, as its application told Windows, names the step when OCR read nothing.
        [Fact]
        public void The_name_Windows_had_is_marked()
        {
            _recording.PressedElements.Add(new PressedElementDto { ActionIndex = 0, Role = "button", Name = "Sign in" });

            Marked("Click <[ Click Sign in ]>").ShouldBe([1]);
        }

        // Withheld, a note says only that a page began or what kind of thing was pressed.
        [Fact]
        public void Without_read_text_the_notes_are_not_marked_and_the_words_searched_by_are()
        {
            _reviewed.Actions.Add(new ReviewedActionDto { ActionIndex = 0, SearchText = "Sign in" });
            string request = string.Join('\n', "    # screen: after the click, a new page", "Search Text <[ Find Sign in ]>");

            ScreenTextLines.Of(request, _recording, _reviewed, false).ShouldBe([2]);
        }

        [Fact]
        public void Text_with_no_letter_or_digit_marks_nothing()
        {
            _recording.ClickedElements.Add(new ClickedElementDto { ActionIndex = 0, Text = " - " });

            Marked("Search Image <[ Find icon ]>", "Wait - 2s").ShouldBeEmpty();
        }
    }
}
