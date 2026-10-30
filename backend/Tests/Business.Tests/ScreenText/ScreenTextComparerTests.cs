using System.Drawing;
using Business.ScreenText;
using Business.ScreenText.Models;

namespace Business.Tests.ScreenText
{
    /// <summary>
    /// What changed between two screenshots by their text, with OCR reading a word differently on
    /// each: the misreadings are from the Firestone screenshots this was measured on.
    /// </summary>
    public sealed class ScreenTextComparerTests
    {
        private static ScreenLine Line(string text, int x, int y)
        {
            return new ScreenLine { Text = text, Bounds = new Rectangle(x, y, text.Length * 8, 16) };
        }

        [Fact]
        public void The_same_screen_changed_nothing()
        {
            List<ScreenLine> screen = [Line("Products", 10, 10), Line("Add to cart", 200, 300)];

            ScreenTextChange change = ScreenTextComparer.Compare(screen, screen);

            change.Appeared.ShouldBeEmpty();
            change.Gone.ShouldBeEmpty();
            change.ChangedShare.ShouldBe(0);
            change.IsNewScreen.ShouldBeFalse();
        }

        [Theory]
        [InlineData("Town", "Towri")]
        [InlineData("Guild", "Gyild")]
        [InlineData("Party", "påfty")]
        public void A_word_misread_where_it_was_is_the_same_word(string read, string misread)
        {
            ScreenTextChange change = ScreenTextComparer.Compare([Line(read, 1670, 194)], [Line(misread, 1671, 196)]);

            change.Appeared.ShouldBeEmpty();
            change.Gone.ShouldBeEmpty();
        }

        [Fact]
        public void A_word_nearly_the_same_somewhere_else_is_a_change()
        {
            ScreenTextChange change = ScreenTextComparer.Compare([Line("Town", 1670, 194)], [Line("Towri", 20, 800)]);

            change.Appeared.ShouldHaveSingleItem().Text.ShouldBe("Towri");
            change.Gone.ShouldHaveSingleItem().Text.ShouldBe("Town");
        }

        [Fact]
        public void The_same_words_anywhere_are_the_same_line_moved()
        {
            ScreenTextChange change = ScreenTextComparer.Compare([Line("Sauce Labs Backpack", 10, 100)], [Line("Sauce Labs Backpack", 10, 600)]);

            change.Appeared.ShouldBeEmpty();
            change.Gone.ShouldBeEmpty();
        }

        // Of six equal buttons, the one that changed is the one in the place of the new one.
        [Fact]
        public void Equal_lines_pair_nearest_first()
        {
            List<ScreenLine> before = Enumerable.Range(0, 6).Select(x => Line("Add to cart", 200, 100 + x * 160)).ToList();
            List<ScreenLine> after = before.Select((x, i) => i == 3 ? Line("Remove", 200, x.Bounds.Y) : x).ToList();

            ScreenTextChange change = ScreenTextComparer.Compare(before, after);

            change.Appeared.ShouldHaveSingleItem().Text.ShouldBe("Remove");
            change.Gone.ShouldHaveSingleItem().Bounds.Y.ShouldBe(100 + 3 * 160);
            change.IsNewScreen.ShouldBeFalse();
        }

        [Fact]
        public void A_number_is_never_nearly_another()
        {
            ScreenTextChange change = ScreenTextComparer.Compare([Line("Items 1", 900, 20)], [Line("Items 2", 900, 20)]);

            change.Appeared.ShouldHaveSingleItem().Text.ShouldBe("Items 2");
            change.Gone.ShouldHaveSingleItem().Text.ShouldBe("Items 1");
        }

        [Fact]
        public void A_number_missed_on_one_side_is_no_difference()
        {
            ScreenTextChange change = ScreenTextComparer.Compare([Line("1 spot in FS!", 18, 824)], [Line("spot in FS!", 26, 826)]);

            change.Appeared.ShouldBeEmpty();
            change.Gone.ShouldBeEmpty();
        }

        // A game's counters change on every screenshot.
        [Fact]
        public void Numbers_and_words_under_four_letters_do_not_count_toward_how_much_changed()
        {
            ScreenTextChange change = ScreenTextComparer.Compare(
                [Line("Products", 10, 10), Line("8014-", 700, 20), Line("62,92itn", 1500, 40)],
                [Line("Products", 10, 10), Line("8021", 700, 20), Line("75,37itn", 1500, 40)]);

            change.Appeared.Select(x => x.Text).ShouldBe(["8021", "75,37itn"]);
            change.ChangedShare.ShouldBe(0);
        }

        [Fact]
        public void Short_words_must_be_read_the_same_to_be_the_same()
        {
            ScreenTextChange change = ScreenTextComparer.Compare([Line("Map", 1677, 309)], [Line("Mop", 1677, 309)]);

            change.Appeared.ShouldHaveSingleItem().Text.ShouldBe("Mop");
        }

        [Fact]
        public void Most_of_the_text_changing_is_a_new_screen()
        {
            List<ScreenLine> login = [Line("Swag Labs", 300, 40), Line("Username", 300, 200), Line("Password", 300, 260), Line("Login", 300, 320)];
            List<ScreenLine> products = [Line("Swag Labs", 300, 40), Line("Products", 10, 120), Line("Sauce Labs Backpack", 200, 200), Line("Add to cart", 200, 300)];

            ScreenTextChange change = ScreenTextComparer.Compare(login, products);

            change.Appeared.Select(x => x.Text).ShouldBe(["Products", "Sauce Labs Backpack", "Add to cart"]);
            change.Gone.Select(x => x.Text).ShouldBe(["Username", "Password", "Login"]);
            change.IsNewScreen.ShouldBeTrue();
        }

        [Fact]
        public void A_line_with_no_letter_or_digit_is_no_text()
        {
            ScreenTextChange change = ScreenTextComparer.Compare([Line("—", 10, 10)], [Line("• •", 10, 10)]);

            change.Appeared.ShouldBeEmpty();
            change.Gone.ShouldBeEmpty();
            change.ChangedShare.ShouldBe(0);
        }
    }
}
