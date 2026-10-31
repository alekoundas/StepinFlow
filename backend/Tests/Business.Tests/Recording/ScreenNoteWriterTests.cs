using Business.Recording.Draft;
using Core.Models.Dtos;

namespace Business.Tests.Recording
{
    /// <summary>What the screen said at a search, as notes for the model and never for the flow.</summary>
    public sealed class ScreenNoteWriterTests
    {
        private static List<string> Notes(int actionIndex, params ScreenChangeDto[] changes)
        {
            return ScreenNoteWriter.NotesFor(actionIndex, new RecordingResultDto { ScreenChanges = changes.ToList() });
        }

        [Fact]
        public void Where_a_new_page_began_the_search_says_so_and_what_was_on_it()
        {
            ScreenChangeDto arrived = new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 3, Appeared = ["Products", "Sauce Labs Backpack"], Gone = ["Username"], IsNewScreen = true };

            Notes(3, arrived).ShouldBe(
            [
                "screen: a new page, most of its words changed since the click before",
                "screen: on it \"Products\", \"Sauce Labs Backpack\"",
            ]);
        }

        [Fact]
        public void What_a_click_made_appear_and_go_is_over_its_search()
        {
            ScreenChangeDto after = new ScreenChangeDto { ActionIndex = 3, NextActionIndex = 5, Appeared = ["Remove"], Gone = ["Add to cart"] };

            Notes(3, after).ShouldBe(["screen: after the click appeared \"Remove\"", "screen: after the click went \"Add to cart\""]);
        }

        // The next search lists the new page, so it is not listed twice.
        [Fact]
        public void A_click_that_opened_a_new_page_says_only_that()
        {
            ScreenChangeDto after = new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 3, Appeared = ["Products"], Gone = ["Login"], IsNewScreen = true };

            Notes(0, after).ShouldBe(["screen: after the click, a new page"]);
        }

        // No search follows the screen the recording stopped on, so the last click lists it.
        [Fact]
        public void A_last_click_that_opened_a_new_page_says_what_is_on_it()
        {
            ScreenChangeDto after = new ScreenChangeDto { ActionIndex = 4, NextActionIndex = null, Appeared = ["Checkout: Complete!", "Thank you for your order!"], Gone = ["Finish"], IsNewScreen = true };

            Notes(4, after).ShouldBe(
            [
                "screen: after the click, a new page, where the recording stopped",
                "screen: on it \"Checkout: Complete!\", \"Thank you for your order!\"",
            ]);
        }

        [Fact]
        public void Counters_and_words_under_four_letters_are_left_out_and_a_long_line_is_cut_short()
        {
            string longLine = "carry.allTheThings() with the sleek, streamlined Sly Pack that melds uncompromising style";
            ScreenChangeDto after = new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 1, Appeared = ["8014-", "Bag", longLine] };

            string note = Notes(0, after).ShouldHaveSingleItem();

            note.ShouldBe("screen: after the click appeared \"carry.allTheThings() with the sleek, streamlined S…\"");
        }

        [Fact]
        public void Past_six_lines_the_rest_are_counted()
        {
            ScreenChangeDto after = new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 1, Appeared = Enumerable.Range(1, 9).Select(x => $"Item number {x}").ToList() };

            Notes(0, after).ShouldHaveSingleItem().ShouldEndWith("\"Item number 6\" and 3 more");
        }

        // A cloud model with screen data off is sent no text, and a new page is a number.
        [Fact]
        public void With_nothing_read_only_the_new_page_is_said()
        {
            ScreenChangeDto arrived = new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 3, IsNewScreen = true };

            Notes(3, arrived).ShouldBe(["screen: a new page, most of its words changed since the click before"]);
        }

        // A single-page application keeps its words and changes its title.
        [Fact]
        public void Another_title_is_a_new_page_whatever_the_words_did()
        {
            ScreenChangeDto title = new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 3, IsNewTitle = true, Title = "Swag Labs - Checkout" };

            Notes(0, title).ShouldBe(["screen: after the click, a new page"]);
            Notes(3, title).ShouldBe(["screen: the window's title changed to \"Swag Labs - Checkout\""]);
        }

        [Fact]
        public void A_title_withheld_still_says_it_changed()
        {
            ScreenChangeDto title = new ScreenChangeDto { ActionIndex = 0, NextActionIndex = 3, IsNewTitle = true };

            Notes(3, title).ShouldBe(["screen: the window's title changed"]);
        }

        [Fact]
        public void What_was_pressed_is_said_as_Windows_named_it()
        {
            PressedElementDto pressed = new PressedElementDto
            {
                ActionIndex = 3,
                Role = "check box",
                Name = "Remember me",
                IsEnabled = false,
                IsChecked = true,
                Inside = ["group \"Sign in\"", "document \"Swag Labs\""],
            };

            List<string> notes = ScreenNoteWriter.NotesFor(3, new RecordingResultDto { PressedElements = [pressed] });

            notes.ShouldBe(["screen: what was pressed, as Windows names it: check box \"Remember me\", not enabled, ticked, in group \"Sign in\", in document \"Swag Labs\""]);
        }

        // Withheld, the name and what it sits in are gone; what kind of thing it was is not screen text.
        [Fact]
        public void What_was_pressed_without_its_name_is_still_its_kind()
        {
            PressedElementDto pressed = new PressedElementDto { ActionIndex = 3, Role = "text field", IsPassword = true };

            List<string> notes = ScreenNoteWriter.NotesFor(3, new RecordingResultDto { PressedElements = [pressed] });

            notes.ShouldBe(["screen: what was pressed, as Windows names it: text field, a password field"]);
        }

        [Fact]
        public void What_the_local_model_sees_goes_with_the_page_and_with_what_was_pressed()
        {
            ScreenDescriptionDto described = new ScreenDescriptionDto { ActionIndex = 3, Page = "The research screen of a fantasy game.", Element = "settings gear" };

            List<string> notes = ScreenNoteWriter.NotesFor(3, new RecordingResultDto { ScreenDescriptions = [described] });

            notes.ShouldBe(
            [
                "screen: the local model sees the page as \"The research screen of a fantasy game.\"",
                "screen: the local model sees what was pressed as \"settings gear\"",
            ]);
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
