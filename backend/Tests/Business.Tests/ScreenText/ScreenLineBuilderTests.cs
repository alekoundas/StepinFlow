using System.Drawing;
using Business.ScreenText;
using Business.ScreenText.Models;
using Core.Models.Business;

namespace Business.Tests.ScreenText
{
    /// <summary>OCR's words into the lines a person reads as one thing.</summary>
    public sealed class ScreenLineBuilderTests
    {
        private static OcrWord Word(string text, int x, int line = 0)
        {
            return new OcrWord { Text = text, Bounds = new Rectangle(x, 100 + line * 40, text.Length * 8, 16), LineIndex = line };
        }

        [Fact]
        public void Words_close_together_on_a_line_are_one_line()
        {
            List<ScreenLine> lines = ScreenLineBuilder.Build([Word("Add", 10), Word("to", 38), Word("cart", 58)]);

            ScreenLine line = lines.ShouldHaveSingleItem();
            line.Text.ShouldBe("Add to cart");
            line.Bounds.ShouldBe(Rectangle.FromLTRB(10, 100, 90, 116));
        }

        // The Windows engine reads three buttons in a row as one line.
        [Fact]
        public void A_gap_wider_than_the_words_are_tall_cuts_the_line()
        {
            List<ScreenLine> lines = ScreenLineBuilder.Build([Word("Bag", 10), Word("Fellowship", 100), Word("Party", 300)]);

            lines.Select(x => x.Text).ShouldBe(["Bag", "Fellowship", "Party"]);
        }

        [Fact]
        public void Each_line_the_engine_read_stays_its_own()
        {
            List<ScreenLine> lines = ScreenLineBuilder.Build([Word("Username", 10, line: 0), Word("Password", 10, line: 1)]);

            lines.Select(x => x.Text).ShouldBe(["Username", "Password"]);
        }

        [Fact]
        public void A_word_with_nothing_in_it_is_left_out()
        {
            List<ScreenLine> lines = ScreenLineBuilder.Build([Word(" ", 10), Word("Login", 20)]);

            lines.ShouldHaveSingleItem().Text.ShouldBe("Login");
        }
    }
}
