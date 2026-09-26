using System.Drawing;

using Business.Searching;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;

namespace Business.Tests.Searching
{
    public sealed class ImageSearcherTests
    {
        private static readonly Rectangle Bounds = new Rectangle(100, 200, 1920, 1080);

        // Fakes
        private readonly FakeScreenshotService _screenshots = new FakeScreenshotService();
        private readonly FakeOpenCvService _matcher = new FakeOpenCvService();


        // ================================================================
        // Private methods
        // ================================================================
        private ImageSearchResult Search(AreaResolution area, IReadOnlyList<SearchTemplate> templates, bool stopAtFirstHit = true, SearchSettings? settings = null)
        {
            return new ImageSearcher(_screenshots, _matcher).Search(area, settings ?? new SearchSettings(), templates, stopAtFirstHit);
        }

        private static AreaResolution Area(ScalesWithEnum scalesWith, int dpi = 96)
        {
            return AreaResolution.Ok(Bounds) with { ScalesWith = scalesWith, Dpi = dpi };
        }

        // The first byte of the image is which template it is, as far as the fake matcher knows.
        private static SearchTemplate Template(byte id, bool isRequired = false, int authoredDpi = 0, int width = 0, int height = 0)
        {
            return new SearchTemplate { Image = [id], Accuracy = 0.8f, IsRequired = isRequired, AuthoredDpi = authoredDpi, AuthoredFlowAreaWidth = width, AuthoredFlowAreaHeight = height };
        }





        // ================================================================
        // How much a template is scaled
        // ================================================================

        [Theory]
        [InlineData(96, 120, 1.25f)]
        [InlineData(120, 96, 0.8f)]
        [InlineData(0, 120, 1f)]
        public void In_a_DPI_area_a_template_scales_by_the_DPI_now_over_the_DPI_it_was_captured_at(int authoredDpi, int dpiNow, float ratio)
        {
            Search(Area(ScalesWithEnum.DPI, dpiNow), [Template(1, authoredDpi: authoredDpi, width: 100, height: 100)]);

            _matcher.Requests.Single().ScaleRatio.ShouldBe(ratio);
        }

        // A game keeps its proportions and adds bars rather than stretching.
        [Fact]
        public void In_an_AREA_area_a_template_scales_by_the_smaller_of_the_two_size_ratios()
        {
            Search(Area(ScalesWithEnum.AREA), [Template(1, width: 960, height: 720)]);

            _matcher.Requests.Single().ScaleRatio.ShouldBe(1.5f);
        }

        [Theory]
        [InlineData(960, 0, 2f)]
        [InlineData(0, 540, 2f)]
        [InlineData(0, 0, 1f)]
        public void In_an_AREA_area_whatever_size_was_recorded_is_used(int width, int height, float ratio)
        {
            Search(Area(ScalesWithEnum.AREA), [Template(1, width: width, height: height)]);

            _matcher.Requests.Single().ScaleRatio.ShouldBe(ratio);
        }

        [Fact]
        public void An_AREA_area_does_not_also_apply_the_DPI()
        {
            Search(Area(ScalesWithEnum.AREA, dpi: 192), [Template(1, authoredDpi: 96, width: 1920, height: 1080)]);

            _matcher.Requests.Single().ScaleRatio.ShouldBe(1f);
        }

        // ================================================================
        // What the matcher is asked
        // ================================================================

        [Fact]
        public void The_mode_is_the_steps_and_the_accuracy_is_each_templates_own()
        {
            SearchTemplate strict = Template(1) with { Accuracy = 0.95f };
            SearchTemplate loose = Template(2) with { Accuracy = 0.7f };

            Search(Area(ScalesWithEnum.DPI), [strict, loose], settings: new SearchSettings { Mode = TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS });

            _matcher.Requests.Select(x => (x.Mode, x.AccuracyThreshold)).ShouldBe(
            [
                (TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS, 0.95f),
                (TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS, 0.7f),
            ]);
        }

        [Theory]
        [InlineData(SearchModeEnum.FIND_ALL, 12)]
        [InlineData(SearchModeEnum.FIND_BEST, 1)]
        [InlineData(SearchModeEnum.WAIT_UNTIL_FOUND, 1)]
        public void Only_FIND_ALL_asks_for_more_than_one_match(SearchModeEnum mode, int maxMatches)
        {
            Search(Area(ScalesWithEnum.DPI), [Template(1)], settings: new SearchSettings { SearchMode = mode, MaxMatches = 12 });

            _matcher.Requests.Single().MaxMatches.ShouldBe(maxMatches);
        }

        [Fact]
        public void The_screenshot_is_of_the_area_and_nothing_else()
        {
            Search(Area(ScalesWithEnum.DPI), [Template(1)]);

            _screenshots.Captured.ShouldBe([Bounds]);
        }

        // ================================================================
        // Required templates
        // ================================================================

        [Fact]
        public void With_none_required_the_first_hit_is_enough_and_ends_the_search()
        {
            _matcher.Found(1, 0.9f);

            ImageSearchResult result = Search(Area(ScalesWithEnum.DPI), [Template(1), Template(2)]);

            result.Hits.Count.ShouldBe(1);
            _matcher.Requests.Count.ShouldBe(1);
        }

        [Fact]
        public void A_missing_required_template_fails_the_search_even_when_others_match()
        {
            _matcher.Found(1, 0.9f);

            ImageSearchResult result = Search(Area(ScalesWithEnum.DPI), [Template(1), Template(2, isRequired: true)]);

            result.Hits.ShouldBeEmpty();
            result.MissingRequired.ShouldBe([1]);
        }

        [Fact]
        public void With_some_required_every_template_is_looked_for_before_deciding()
        {
            _matcher.Found(1, 0.9f).Found(2, 0.9f);

            ImageSearchResult result = Search(Area(ScalesWithEnum.DPI), [Template(1), Template(2, isRequired: true)]);

            _matcher.Requests.Count.ShouldBe(2);
            result.Hits.Count.ShouldBe(1);
        }

        [Fact]
        public void Every_required_template_found_and_not_stopping_keeps_every_hit()
        {
            _matcher.Found(1, 0.9f).Found(2, 0.9f);

            ImageSearchResult result = Search(Area(ScalesWithEnum.DPI), [Template(1, isRequired: true), Template(2, isRequired: true)], stopAtFirstHit: false);

            result.Hits.Count.ShouldBe(2);
            result.MissingRequired.ShouldBeEmpty();
        }

        // ================================================================
        // What comes back
        // ================================================================

        [Fact]
        public void A_hit_is_where_to_click_with_the_offset_scaled_like_the_template()
        {
            _matcher.Found(1, 0.9f, x: 5, y: 5, scale: 2f);
            SearchTemplate template = Template(1) with { ClickOffsetX = 10, ClickOffsetY = 4 };

            ImageSearchResult result = Search(Area(ScalesWithEnum.DPI), [template]);

            result.Hits.ShouldBe([new Point(25, 13)]);
        }

        [Fact]
        public void The_closest_score_and_which_template_reached_it_are_kept_when_nothing_passes()
        {
            _matcher.NotFound(1, 0.4f).NotFound(2, 0.78f).NotFound(3, 0.6f);

            ImageSearchResult result = Search(Area(ScalesWithEnum.DPI), [Template(1), Template(2), Template(3)]);

            result.Hits.ShouldBeEmpty();
            result.BestScore.ShouldBe(0.78f);
            result.BestTemplateIndex.ShouldBe(1);
        }

        [Fact]
        public void A_template_that_cannot_be_searched_fails_the_search_naming_it()
        {
            _matcher.Answer(2, new TemplateMatchOutcome { Error = "scaled by 4.00 it is bigger than the area." });

            ImageSearchResult result = Search(Area(ScalesWithEnum.DPI), [Template(1), Template(2)]);

            result.Error.ShouldBe("Template 2: scaled by 4.00 it is bigger than the area.");
        }

        [Fact]
        public void An_area_with_no_size_is_an_error_not_an_empty_search()
        {
            ImageSearchResult result = Search(AreaResolution.Ok(Rectangle.Empty), [Template(1)]);

            result.Error.ShouldNotBeNull();
            _screenshots.Captured.ShouldBeEmpty();
        }
    }
}
