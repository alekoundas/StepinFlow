using System.Drawing;
using Business.Recording.Models.Session;
using Business.Recording.Screens;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;
using static Business.Tests.TestToken;

namespace Business.Tests.Recording
{
    /// <summary>Every press's screenshot read whole, and each compared with the one before it.</summary>
    public sealed class RecordedScreenReaderTests
    {
        private readonly FakeOcrService _ocr = new FakeOcrService();
        private readonly RecordedScreenReader _reader;
        private readonly Dictionary<int, RecordedPress> _presses = new Dictionary<int, RecordedPress>();

        public RecordedScreenReaderTests()
        {
            _reader = new RecordedScreenReader(_ocr, new FakeScreenshotService());
        }

        private RecordedActionDto Click(int index)
        {
            _presses[index] = new RecordedPress { Index = index, MainAreaBounds = new Rectangle(0, 0, 800, 600), Dpi = 96, Screenshot = [1] };
            return new RecordedActionDto { Index = index, Kind = RecordedActionKindEnum.CLICK, ScreenshotIndex = index };
        }

        private static IReadOnlyList<OcrWord> Screen(params string[] lines)
        {
            return lines.Select((x, i) => new OcrWord { Text = x, Bounds = new Rectangle(10, 10 + i * 30, x.Length * 8, 14), LineIndex = i }).ToList();
        }

        private Task<IReadOnlyList<ScreenChangeDto>> ReadAsync(params RecordedActionDto[] actions)
        {
            return _reader.ReadAsync(actions, x => _presses.GetValueOrDefault(x), (done, total) => { }, Ct);
        }

        [Fact]
        public async Task Each_press_is_compared_with_the_one_before()
        {
            _ocr.ReadsWords(
                Screen("Username", "Password", "Login"),
                Screen("Products", "Sauce Labs Backpack", "Add to cart"),
                Screen("Products", "Sauce Labs Backpack", "Remove"));

            IReadOnlyList<ScreenChangeDto> changes = await ReadAsync(Click(0), Click(3), Click(5));

            changes.Select(x => (x.ActionIndex, x.NextActionIndex)).ShouldBe([(0, 3), (3, 5)]);
            changes[0].IsNewScreen.ShouldBeTrue();
            changes[1].Appeared.ShouldBe(["Remove"]);
            changes[1].Gone.ShouldBe(["Add to cart"]);
            changes[1].IsNewScreen.ShouldBeFalse();
        }

        // Typing keeps no screenshot of its own: what it did shows at the next press.
        [Fact]
        public async Task An_action_with_no_press_is_passed_over()
        {
            _ocr.ReadsWords(Screen("Username"), Screen("Products"));
            RecordedActionDto typing = new RecordedActionDto { Index = 1, Kind = RecordedActionKindEnum.TYPING };

            IReadOnlyList<ScreenChangeDto> changes = await ReadAsync(Click(0), typing, Click(2));

            ScreenChangeDto change = changes.ShouldHaveSingleItem();
            (change.ActionIndex, change.NextActionIndex).ShouldBe((0, 2));
            _ocr.WordReadCount.ShouldBe(2);
        }

        [Fact]
        public async Task One_press_has_nothing_to_compare_with()
        {
            IReadOnlyList<ScreenChangeDto> changes = await ReadAsync(Click(0));

            changes.ShouldBeEmpty();
            _ocr.WordReadCount.ShouldBe(1);
        }
    }
}
