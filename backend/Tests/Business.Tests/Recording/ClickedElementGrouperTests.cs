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
    /// Which clicks hit the same picture. Whether two pictures are the same is the matcher's,
    /// faked here as "everything passed in together is"; what is passed in together is what this
    /// decides.
    /// </summary>
    public sealed class ClickedElementGrouperTests
    {
        private readonly FakeOpenCvService _openCv = new FakeOpenCvService();
        private readonly FakeScreenshotService _screenshots = new FakeScreenshotService();
        private readonly ClickedElementGrouper _grouper;
        private readonly List<RecordedActionDto> _actions = new List<RecordedActionDto>();
        private readonly Dictionary<int, RecordedPress> _presses = new Dictionary<int, RecordedPress>();

        public ClickedElementGrouperTests()
        {
            _grouper = new ClickedElementGrouper(_openCv, _screenshots);
        }

        private ClickedElementDto Element(int index, int x, int y, string text = "", ElementFoundByEnum foundBy = ElementFoundByEnum.MODEL, int dpi = 96)
        {
            _actions.Add(new RecordedActionDto { Index = index, Kind = RecordedActionKindEnum.CLICK, ScreenshotIndex = index });
            _presses[index] = new RecordedPress { Index = index, MainAreaBounds = new Rectangle(0, 0, 800, 600), Dpi = dpi, Screenshot = [1] };

            return new ClickedElementDto { ActionIndex = index, X = x, Y = y, Width = 40, Height = 20, Text = text, FoundBy = foundBy, Image = [2] };
        }

        private IReadOnlyList<ClickGroupDto> Group(params ClickedElementDto[] elements)
        {
            return _grouper.Group(elements, _actions, x => _presses.GetValueOrDefault(x), (done, total) => { }, Ct);
        }

        [Fact]
        public void The_same_picture_in_four_places_is_one_moving_group()
        {
            IReadOnlyList<ClickGroupDto> groups = Group(Element(9, 660, 160), Element(10, 660, 198), Element(11, 660, 236), Element(12, 660, 274));

            ClickGroupDto group = groups.ShouldHaveSingleItem();
            group.ActionIndexes.ShouldBe([9, 10, 11, 12]);
            group.IsMoving.ShouldBeTrue();
        }

        [Fact]
        public void One_place_clicked_again_is_a_group_that_does_not_move()
        {
            IReadOnlyList<ClickGroupDto> groups = Group(Element(0, 300, 200, "Next"), Element(1, 304, 201, "Next"));

            groups.ShouldHaveSingleItem().IsMoving.ShouldBeFalse();
        }

        // A box around the click carries background into the comparison, as a crop does.
        [Fact]
        public void Only_the_elements_the_model_found_are_compared()
        {
            IReadOnlyList<ClickGroupDto> groups = Group(Element(0, 100, 100), Element(1, 100, 140, foundBy: ElementFoundByEnum.FALLBACK), Element(2, 100, 180));

            _openCv.Grouped.ShouldHaveSingleItem().Count.ShouldBe(2);
            groups.ShouldHaveSingleItem().ActionIndexes.ShouldBe([0, 2]);
        }

        [Fact]
        public void Two_elements_whose_read_text_differs_never_group()
        {
            IReadOnlyList<ClickGroupDto> groups = Group(Element(0, 100, 100, "Save"), Element(1, 160, 100, "Send"));

            groups.ShouldBeEmpty();
            _openCv.Grouped.Select(x => x.Count).ShouldBe([1, 1]);
        }

        [Fact]
        public void Read_text_is_compared_without_its_case()
        {
            IReadOnlyList<ClickGroupDto> groups = Group(Element(0, 100, 100, "Save"), Element(1, 100, 300, "SAVE"));

            groups.ShouldHaveSingleItem().ActionIndexes.ShouldBe([0, 1]);
        }

        // Enough for a box drawn a few pixels off the other, as many at twice the dpi.
        [Fact]
        public void Each_element_is_compared_inside_its_rectangle_widened_by_a_few_pixels()
        {
            Group(Element(0, 100, 100, dpi: 192), Element(1, 100, 300, dpi: 192));

            _screenshots.Encoded.Select(x => (x.Width, x.Height)).ShouldBe([(64, 44), (64, 44)]);
        }

        [Fact]
        public void A_click_whose_press_is_gone_is_left_out()
        {
            ClickedElementDto first = Element(0, 100, 100);
            ClickedElementDto second = Element(1, 100, 300);
            _presses.Remove(1);

            Group(first, second).ShouldBeEmpty();
            _openCv.Grouped.ShouldHaveSingleItem().Count.ShouldBe(1);
        }
    }
}
