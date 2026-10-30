using Business.Recording.Draft;
using Core.Models.Dtos;

namespace Business.Tests.Recording
{
    /// <summary>What the screen said at a search, as notes for the model and never for the flow.</summary>
    public sealed class ScreenNoteWriterTests
    {
        [Fact]
        public void Where_a_new_page_began_the_search_says_so_and_what_was_on_it()
        {
            ScreenChangeDto arrived = new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 3, Appeared = ["Products", "Sauce Labs Backpack"], Gone = ["Username"], IsNewScreen = true };

            List<string> notes = ScreenNoteWriter.NotesFor(3, [arrived]);

            notes.ShouldBe(
            [
                "screen: a new page, most of its words changed since the click before",
                "screen: on it \"Products\", \"Sauce Labs Backpack\"",
            ]);
        }

        [Fact]
        public void What_a_click_made_appear_and_go_is_over_its_search()
        {
            ScreenChangeDto after = new ScreenChangeDto { ActionIndex = 3, NextActionIndex = 5, Appeared = ["Remove"], Gone = ["Add to cart"] };

            List<string> notes = ScreenNoteWriter.NotesFor(3, [after]);

            notes.ShouldBe(["screen: after the click appeared \"Remove\"", "screen: after the click went \"Add to cart\""]);
        }

        // The next search lists the new page, so it is not listed twice.
        [Fact]
        public void A_click_that_opened_a_new_page_says_only_that()
        {
            ScreenChangeDto after = new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 3, Appeared = ["Products"], Gone = ["Login"], IsNewScreen = true };

            ScreenNoteWriter.NotesFor(0, [after]).ShouldBe(["screen: after the click, a new page"]);
        }

        [Fact]
        public void Counters_and_words_under_four_letters_are_left_out_and_a_long_line_is_cut_short()
        {
            string longLine = "carry.allTheThings() with the sleek, streamlined Sly Pack that melds uncompromising style";
            ScreenChangeDto after = new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 1, Appeared = ["8014-", "Bag", longLine] };

            string note = ScreenNoteWriter.NotesFor(0, [after]).ShouldHaveSingleItem();

            note.ShouldBe("screen: after the click appeared \"carry.allTheThings() with the sleek, streamlined S…\"");
        }

        [Fact]
        public void Past_six_lines_the_rest_are_counted()
        {
            ScreenChangeDto after = new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 1, Appeared = Enumerable.Range(1, 9).Select(x => $"Item number {x}").ToList() };

            ScreenNoteWriter.NotesFor(0, [after]).ShouldHaveSingleItem().ShouldEndWith("\"Item number 6\" and 3 more");
        }

        // A cloud model with screen data off is sent no text, and a new page is a number.
        [Fact]
        public void With_nothing_read_only_the_new_page_is_said()
        {
            ScreenChangeDto arrived = new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 3, IsNewScreen = true };

            ScreenNoteWriter.NotesFor(3, [arrived]).ShouldBe(["screen: a new page, most of its words changed since the click before"]);
        }

        [Fact]
        public void Every_note_comes_out_of_a_script_and_nothing_else_does()
        {
            string script = "Steps:\r\n# recorded after a 1.2s wait\r\n# screen: on it \"Products\"\r\n## Sign in\r\n #  Screen: after the click went \"Login\"\r\nFind Image      <[ Find Login ]>\r\n";

            string cleaned = ScreenNoteWriter.WithoutNotes(script);

            cleaned.ShouldBe("Steps:\r\n# recorded after a 1.2s wait\r\n## Sign in\r\nFind Image      <[ Find Login ]>\r\n");
        }
    }
}
