using Platform.Shared.Ocr.CharacterExtractor;
using Platform.Shared.Ocr.Text;

namespace Platform.Shared.Tests.Ocr
{
    /// <summary>
    /// The parts of reading text that need no model.
    /// Test:
    /// 1) A reader's character list: CTC's blank first, a space last, YAML's quoting undone, an empty entry and an escape JSON lacks refused
    /// 2) A word's edges are the widest blank run between its characters and its neighbour's
    /// </summary>
    public sealed class OcrPartsTests : IDisposable
    {
        private readonly string _yml = Path.Combine(Path.GetTempPath(), $"StepinFlowTests-{Guid.NewGuid():N}.yml");

        public void Dispose()
        {
            File.Delete(_yml);
        }

        [Fact]
        public void The_character_list_has_the_blank_first_and_a_space_last()
        {
            File.WriteAllLines(_yml,
            [
                "PostProcess:",
                "  name: CTCLabelDecode",
                "  character_dict:",
                "  - '0'",
                "  - a",
                "  - ''''",
                "  - \"\\u03A9\"",
                "  - 🕣",
            ]);

            OcrCharacterExtractor.Load(_yml).ShouldBe(["", "0", "a", "'", "Ω", "🕣", " "]);
        }

        [Fact]
        public void A_list_with_no_characters_is_refused()
        {
            File.WriteAllLines(_yml, ["Global:", "  model_name: something"]);

            Should.Throw<InvalidDataException>(() => OcrCharacterExtractor.Load(_yml));
        }

        [Fact]
        public void An_empty_entry_is_refused()
        {
            File.WriteAllLines(_yml, ["  character_dict:", "  - a", "  -", "  - b"]);

            Should.Throw<InvalidDataException>(() => OcrCharacterExtractor.Load(_yml)).Message.ShouldContain("empty entry");
        }

        [Fact]
        public void An_escape_only_yaml_has_is_refused()
        {
            File.WriteAllLines(_yml, ["  character_dict:", "  - a", "  - \"\\x41\""]);

            Should.Throw<InvalidDataException>(() => OcrCharacterExtractor.Load(_yml)).Message.ShouldContain("escape");
        }

        //           0123456789...
        // columns:  .xxx..xx....xxx.
        [Fact]
        public void The_gap_between_two_words_is_the_widest_blank_run_between_them()
        {
            bool[] columns = Columns(".xxx..xx....xxx.");

            WordEdges.WidestGap(columns, 2, 13).ShouldBe((8, 11));
        }

        [Fact]
        public void Letters_touching_have_no_gap()
        {
            WordEdges.WidestGap(Columns("xxxxxx"), 1, 4).ShouldBe((-1, -1));
        }

        [Fact]
        public void A_line_starts_and_ends_at_its_first_and_last_text()
        {
            bool[] columns = Columns("..xx..xx..");

            WordEdges.FirstInk(columns, 5).ShouldBe(2);
            WordEdges.LastInk(columns, 4).ShouldBe(7);
        }

        [Fact]
        public void Ink_is_what_stands_out_from_the_background_however_faint()
        {
            // Mid grey text on light grey: 150 on 200.
            byte[] gray = [200, 200, 150, 200, 200, 150];

            bool[,] ink = WordEdges.InkOf(gray, 3, 2);

            WordEdges.ColumnsOf(ink).ShouldBe([false, false, true]);
            WordEdges.RowsOf(ink, 2, 2).ShouldBe((0, 1));
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static bool[] Columns(string drawn)
        {
            return drawn.Select(x => x == 'x').ToArray();
        }
    }
}
