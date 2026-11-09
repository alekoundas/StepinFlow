using System.Drawing;
using Business.Recording.Elements;
using Business.Recording.Models.Session;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Dtos;
using static Business.Tests.TestToken;

namespace Business.Tests.Recording
{
    /// <summary>
    /// What each click hit: found in the main area's screenshot from before the press, at the
    /// press's place inside it, with the text on it read.
    /// </summary>
    public sealed class ClickedElementFinderTests : IDisposable
    {
        private static readonly Rectangle MAIN_AREA = new Rectangle(100, 50, 800, 600);

        private readonly FakeElementLocator _locator = new FakeElementLocator();
        private readonly FakeOcrService _ocr = new FakeOcrService();
        private readonly ClickedElementFinder _finder;
        private readonly Dictionary<int, RecordedPress> _presses = new Dictionary<int, RecordedPress>();
        private readonly Dictionary<int, ClickedElementDto> _foundWhileAsking = new Dictionary<int, ClickedElementDto>();

        public ClickedElementFinderTests()
        {
            _finder = new ClickedElementFinder(_locator, _ocr, new FakeScreenshotService());
        }

        public void Dispose()
        {
            _finder.Dispose();
        }

        private RecordedActionDto Click(int index, int screenX, int screenY, RecordedActionKindEnum kind = RecordedActionKindEnum.CLICK)
        {
            _presses[index] = new RecordedPress { Index = index, MainAreaBounds = MAIN_AREA, Dpi = 96, Screenshot = [1] };
            return new RecordedActionDto { Index = index, Kind = kind, LocationX = screenX, LocationY = screenY, ScreenshotIndex = index, PressIndex = index };
        }

        private Task<IReadOnlyList<ClickedElementDto>> FindAsync(params RecordedActionDto[] actions)
        {
            return _finder.FindAsync(actions, x => _presses.GetValueOrDefault(x), _foundWhileAsking, (done, total) => { }, Ct);
        }

        [Fact]
        public async Task A_click_is_found_at_its_place_in_the_main_area_and_its_text_read()
        {
            _ocr.Reads("Sign  in\n");

            IReadOnlyList<ClickedElementDto> elements = await FindAsync(Click(0, 400, 350));

            _locator.Located.ShouldBe([new Point(300, 300)]);
            ClickedElementDto found = elements.ShouldHaveSingleItem();
            (found.ActionIndex, found.X, found.Y, found.Width, found.Height).ShouldBe((0, 280, 290, 40, 20));
            found.Text.ShouldBe("Sign in");
            found.FoundBy.ShouldBe(ElementFoundByEnum.MODEL);
            found.Image.ShouldNotBeEmpty();
        }

        // Faint text on a small button often reads only when enlarged.
        [Fact]
        public async Task Text_that_does_not_read_is_read_again_at_twice_the_size()
        {
            _ocr.Reads("", "+ New");

            IReadOnlyList<ClickedElementDto> elements = await FindAsync(Click(0, 400, 350));

            elements.Single().Text.ShouldBe("+ New");
            _ocr.ReadCount.ShouldBe(2);
        }

        [Fact]
        public async Task A_press_outside_the_main_area_finds_nothing()
        {
            IReadOnlyList<ClickedElementDto> elements = await FindAsync(Click(0, 20, 20));

            elements.ShouldBeEmpty();
            _locator.Located.ShouldBeEmpty();
        }

        [Fact]
        public async Task Only_clicks_and_drags_with_a_screenshot_are_looked_at()
        {
            RecordedActionDto typing = new RecordedActionDto { Index = 1, Kind = RecordedActionKindEnum.TYPING };
            RecordedActionDto withoutScreenshot = new RecordedActionDto { Index = 2, Kind = RecordedActionKindEnum.CLICK, LocationX = 400, LocationY = 350 };

            IReadOnlyList<ClickedElementDto> elements = await FindAsync(Click(0, 400, 350), typing, withoutScreenshot, Click(3, 500, 400, RecordedActionKindEnum.DRAG));

            elements.Select(x => x.ActionIndex).ShouldBe([0, 3]);
        }

        // Hundreds of megabytes the app has no use for until the next recording.
        [Fact]
        public async Task The_model_is_let_go_after_the_batch()
        {
            await FindAsync(Click(0, 400, 350));

            _locator.IsLoaded.ShouldBeFalse();
            _locator.UnloadCount.ShouldBe(1);
        }

        // ================================================================
        // A check asked about while the recording is paused
        // ================================================================

        [Fact]
        public async Task A_press_is_found_while_asking_and_the_model_stays_loaded_for_the_next()
        {
            _ocr.Reads("Remove");
            RecordedActionDto check = Click(0, 400, 350, RecordedActionKindEnum.CHECK);

            ClickedElementDto? found = await _finder.FindAtAsync(_presses[0], check.LocationX, check.LocationY, Ct);

            found.ShouldNotBeNull().Text.ShouldBe("Remove");
            (found.X, found.Y).ShouldBe((280, 290));
            _locator.IsLoaded.ShouldBeTrue();
            _locator.UnloadCount.ShouldBe(0);
        }

        // Found once, and given its action's number, which it had none of while recording.
        [Fact]
        public async Task The_stop_takes_a_press_found_while_asking_as_it_was_and_lets_the_model_go()
        {
            _ocr.Reads("Remove", "Checkout");
            RecordedActionDto check = Click(0, 400, 350, RecordedActionKindEnum.CHECK);
            _foundWhileAsking[0] = (await _finder.FindAtAsync(_presses[0], check.LocationX, check.LocationY, Ct))!;
            check.Index = 4;

            IReadOnlyList<ClickedElementDto> elements = await FindAsync(check, Click(5, 500, 400));

            elements.Select(x => (x.ActionIndex, x.Text)).ShouldBe([(4, "Remove"), (5, "Checkout")]);
            _locator.Located.Count.ShouldBe(2);
            _locator.IsLoaded.ShouldBeFalse();
        }

        [Fact]
        public async Task A_recording_discarded_with_the_model_loaded_lets_it_go()
        {
            RecordedActionDto check = Click(0, 400, 350, RecordedActionKindEnum.CHECK);
            await _finder.FindAtAsync(_presses[0], check.LocationX, check.LocationY, Ct);

            _finder.Unload();

            _locator.IsLoaded.ShouldBeFalse();
        }
    }
}
