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

        private const int LAST_SCREEN_DPI = 144;

        // What the fake OCR reads does not depend on the picture.
        private static readonly RecordingScreenshot LAST_SCREEN = new RecordingScreenshot(
            new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc),
            new RecordingTarget(new Rectangle(0, 0, 800, 600), LAST_SCREEN_DPI, WindowHandle.None, 0),
            new RawImage { Width = 1, Height = 1, Stride = 4, Pixels = [0, 0, 0, 255] });

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
            RecordedScreens screens = await _reader.ReadAsync(actions, x => _presses.GetValueOrDefault(x), [], null, (done, total) => { }, Ct);
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

            RecordedScreens screens = await _reader.ReadAsync([finish], x => _presses.GetValueOrDefault(x), [], LAST_SCREEN, (done, total) => progress.Add((done, total)), Ct);

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

            RecordedScreens screens = await _reader.ReadAsync([Click(0)], x => _presses.GetValueOrDefault(x), [], LAST_SCREEN, (done, total) => { }, Ct);

            screens.Lines.ShouldBe(["Products", "Add to cart", "Remove"]);
        }

        // A screen scaled to 150% draws text half as large again, which the reader allows for.
        [Fact]
        public async Task Each_screen_is_read_at_the_dpi_it_was_captured_at()
        {
            RecordedActionDto click = Click(0);
            _presses[0] = new RecordedPress { Index = 0, MainAreaBounds = new Rectangle(0, 0, 800, 600), Dpi = 120, Screenshot = [1] };

            await _reader.ReadAsync([click], x => _presses.GetValueOrDefault(x), [], null, (done, total) => { }, Ct);
            _ocr.LastDpi.ShouldBe(120);

            await _reader.ReadAsync([click], x => _presses.GetValueOrDefault(x), [], LAST_SCREEN, (done, total) => { }, Ct);
            _ocr.LastDpi.ShouldBe(LAST_SCREEN_DPI);
        }

        // ================================================================
        // Which page each press was on
        // ================================================================

        private async Task<List<int>> PagesAsync(params RecordedActionDto[] actions)
        {
            RecordedScreens screens = await _reader.ReadAsync(actions, x => _presses.GetValueOrDefault(x), [], null, (done, total) => { }, Ct);
            screens.Pages.Select(x => x.ActionIndex).ShouldBe(actions.Select(x => x.Index));

            return screens.Pages.Select(x => x.Page).ToList();
        }

        [Fact]
        public async Task A_page_stays_the_same_page_while_only_some_of_it_changes()
        {
            _ocr.ReadsWords(
                Screen("Products", "Sauce Labs Backpack", "Add to cart"),
                Screen("Products", "Sauce Labs Backpack", "Remove"));

            (await PagesAsync(Click(0), Click(1))).ShouldBe([1, 1]);
        }

        // A menu left or a dialog closed is the page it was opened from, not one more.
        [Fact]
        public async Task A_page_come_back_to_has_the_number_it_had()
        {
            _ocr.ReadsWords(
                Screen("Username", "Password", "Login"),
                Screen("Products", "Sauce Labs Backpack", "Add to cart"),
                Screen("Your Cart", "Checkout", "Continue Shopping"),
                Screen("Products", "Sauce Labs Backpack", "Remove"),
                Screen("Username", "Password", "Login"));

            (await PagesAsync(Click(0), Click(1), Click(2), Click(3), Click(4))).ShouldBe([1, 2, 3, 2, 1]);
        }

        // A single-page application keeps most of its words and changes its title.
        [Fact]
        public async Task The_same_words_under_another_title_are_another_page()
        {
            RecordedActionDto inbox = Click(0);
            (inbox.Window, inbox.WindowTitle) = (RecordedWindowEnum.MAIN_AREA, "Inbox");
            RecordedActionDto sent = Click(1);
            (sent.Window, sent.WindowTitle) = (RecordedWindowEnum.MAIN_AREA, "Sent");
            RecordedActionDto back = Click(2);
            (back.Window, back.WindowTitle) = (RecordedWindowEnum.MAIN_AREA, "Inbox");

            (await PagesAsync(inbox, sent, back)).ShouldBe([1, 2, 1]);
        }

        // ================================================================
        // The line under a click
        // ================================================================

        // A click in the main area, which is at the screen's corner here.
        private RecordedActionDto ClickAt(int index, int x, int y)
        {
            RecordedActionDto click = Click(index);
            (click.LocationX, click.LocationY) = (x, y);

            return click;
        }

        // The element's rectangle may cut a word in half; the whole screenshot does not.
        [Fact]
        public async Task The_line_under_a_click_is_read_whole()
        {
            _ocr.ReadsWords(Screen("Accepted usernames", "standard_user", "Login"));
            RecordedActionDto click = ClickAt(0, 20, 45);

            RecordedScreens screens = await _reader.ReadAsync([click], x => _presses.GetValueOrDefault(x), [], null, (done, total) => { }, Ct);

            screens.LineTexts[0].ShouldBe("standard_user");
        }

        // A text field is clicked in its middle, beside its placeholder.
        [Fact]
        public async Task A_click_beside_its_words_takes_the_nearest_line_its_element_touches()
        {
            _ocr.ReadsWords(Screen("Username", "Password", "Login"));
            RecordedActionDto click = ClickAt(0, 300, 47);
            ClickedElementDto field = new ClickedElementDto { ActionIndex = 0, X = 5, Y = 35, Width = 400, Height = 24 };

            RecordedScreens screens = await _reader.ReadAsync([click], x => _presses.GetValueOrDefault(x), [field], null, (done, total) => { }, Ct);

            screens.LineTexts[0].ShouldBe("Password");
        }

        [Fact]
        public async Task A_click_on_no_words_has_no_line()
        {
            _ocr.ReadsWords(Screen("Username", "Password", "Login"));
            RecordedActionDto click = ClickAt(0, 600, 400);
            ClickedElementDto icon = new ClickedElementDto { ActionIndex = 0, X = 580, Y = 380, Width = 40, Height = 40 };

            RecordedScreens screens = await _reader.ReadAsync([click], x => _presses.GetValueOrDefault(x), [icon], null, (done, total) => { }, Ct);

            screens.LineTexts.ShouldBeEmpty();
        }
    }
}
