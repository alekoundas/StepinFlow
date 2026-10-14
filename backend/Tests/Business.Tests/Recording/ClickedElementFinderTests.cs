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
    public sealed class ClickedElementFinderTests
    {
        private static readonly Rectangle MAIN_AREA = new Rectangle(100, 50, 800, 600);

        private readonly FakeElementLocator _locator = new FakeElementLocator();
        private readonly FakeOcrService _ocr = new FakeOcrService();
        private readonly ClickedElementFinder _finder;
        private readonly Dictionary<int, RecordedPress> _presses = new Dictionary<int, RecordedPress>();

        public ClickedElementFinderTests()
        {
            _finder = new ClickedElementFinder(_locator, _ocr, new FakeScreenshotService());
        }

        private RecordedActionDto Click(int index, int screenX, int screenY, RecordedActionKindEnum kind = RecordedActionKindEnum.CLICK)
        {
            _presses[index] = new RecordedPress { Index = index, MainAreaBounds = MAIN_AREA, Dpi = 96, Screenshot = [1] };
            return new RecordedActionDto { Index = index, Kind = kind, LocationX = screenX, LocationY = screenY, ScreenshotIndex = index };
        }

        private Task<IReadOnlyList<ClickedElementDto>> FindAsync(params RecordedActionDto[] actions)
        {
            return _finder.FindAsync(actions, x => _presses.GetValueOrDefault(x), (done, total) => { }, Ct);
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
    }
}
