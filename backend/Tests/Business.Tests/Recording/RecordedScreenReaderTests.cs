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
    /// <summary>Every press's screenshot read whole, and each compared with the one before it, the last with the screen the recording stopped on.</summary>
    public sealed class RecordedScreenReaderTests
    {
        private readonly FakeOcrService _ocr = new FakeOcrService();
        private readonly RecordedScreenReader _reader;
        private readonly Dictionary<int, RecordedPress> _presses = new Dictionary<int, RecordedPress>();

        // What the fake OCR reads does not depend on the picture.
        private static readonly RawImage LAST_SCREEN = new RawImage { Width = 1, Height = 1, Stride = 4, Pixels = [0, 0, 0, 255] };

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

        private async Task<List<ScreenChangeDto>> ReadAsync(params RecordedActionDto[] actions)
        {
            RecordedScreens screens = await _reader.ReadAsync(actions, x => _presses.GetValueOrDefault(x), null, (done, total) => { }, Ct);
            return screens.Changes;
        }

        [Fact]
        public async Task Each_press_is_compared_with_the_one_before()
        {
            _ocr.ReadsWords(
                Screen("Username", "Password", "Login"),
                Screen("Products", "Sauce Labs Backpack", "Add to cart"),
                Screen("Products", "Sauce Labs Backpack", "Remove"));

            List<ScreenChangeDto> changes = await ReadAsync(Click(0), Click(3), Click(5));

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

            List<ScreenChangeDto> changes = await ReadAsync(Click(0), typing, Click(2));

            ScreenChangeDto change = changes.ShouldHaveSingleItem();
            (change.ActionIndex, change.NextActionIndex).ShouldBe((0, 2));
            _ocr.WordReadCount.ShouldBe(2);
        }

        // For a browser, the title is the page's.
        [Fact]
        public async Task Another_title_on_the_main_area_s_window_is_a_new_title()
        {
            RecordedActionDto login = Click(0);
            login.Window = RecordedWindowEnum.MAIN_AREA;
            login.WindowTitle = "Swag Labs - Login";
            RecordedActionDto products = Click(2);
            products.Window = RecordedWindowEnum.MAIN_AREA;
            products.WindowTitle = "Swag Labs - Products";
            RecordedActionDto taskbar = Click(4);
            taskbar.Window = RecordedWindowEnum.OTHER;
            taskbar.WindowTitle = "Taskbar";

            List<ScreenChangeDto> changes = await ReadAsync(login, products, taskbar);

            (changes[0].IsNewTitle, changes[0].Title).ShouldBe((true, "Swag Labs - Products"));
            (changes[1].IsNewTitle, changes[1].Title).ShouldBe((false, null));
        }

        [Fact]
        public async Task One_press_has_nothing_to_compare_with()
        {
            List<ScreenChangeDto> changes = await ReadAsync(Click(0));

            changes.ShouldBeEmpty();
            _ocr.WordReadCount.ShouldBe(1);
        }

        // What the last click did shows only on the screen the recording stopped on.
        [Fact]
        public async Task The_last_press_is_compared_with_the_screen_the_recording_stopped_on()
        {
            _ocr.ReadsWords(
                Screen("Checkout: Overview", "Finish"),
                Screen("Checkout: Complete!", "Thank you for your order!", "Back Home"));
            RecordedActionDto finish = Click(4);
            finish.Window = RecordedWindowEnum.MAIN_AREA;
            finish.WindowTitle = "Swag Labs";
            List<(int, int)> progress = [];

            RecordedScreens screens = await _reader.ReadAsync([finish], x => _presses.GetValueOrDefault(x), LAST_SCREEN, (done, total) => progress.Add((done, total)), Ct);

            ScreenChangeDto change = screens.Changes.ShouldHaveSingleItem();
            (change.ActionIndex, change.NextActionIndex).ShouldBe((4, null));
            change.Appeared.ShouldContain("Thank you for your order!");
            (change.IsNewTitle, change.Title).ShouldBe((false, null));
            progress[^1].ShouldBe((2, 2));
        }

        [Fact]
        public async Task Every_line_read_is_kept_once_in_the_order_first_read()
        {
            _ocr.ReadsWords(
                Screen("Products", "Add to cart"),
                Screen("Products", "Remove"));

            RecordedScreens screens = await _reader.ReadAsync([Click(0)], x => _presses.GetValueOrDefault(x), LAST_SCREEN, (done, total) => { }, Ct);

            screens.Lines.ShouldBe(["Products", "Add to cart", "Remove"]);
        }
    }
}
