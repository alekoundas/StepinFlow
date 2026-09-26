using System.Drawing;

using Business.Executions;
using Business.Executions.Workers;
using Business.Searching;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;

using Microsoft.Extensions.Time.Testing;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// The worker around <see cref="ImageSearcher"/>: the poll loop, the click point in screen
    /// coordinates, and what a miss says. The matching rules themselves are
    /// <c>Searching/ImageSearcherTests</c> - here the subject is what the step reports, because a
    /// failure that does not name the closest template and its bar cannot be acted on.
    /// </summary>
    public sealed class SearchImageStepWorkerTests
    {
        private static readonly Rectangle Bounds = new Rectangle(100, 200, 800, 600);

        private readonly FakeScreenshotService _screenshots = new FakeScreenshotService();
        private readonly FakeOpenCvService _matcher = new FakeOpenCvService();
        private readonly FakeAreaPointResolver _resolver = new FakeAreaPointResolver();

        // Every read of the clock moves it a second, so a wait with a timeout gives up after a few
        // polls instead of after real seconds.
        private readonly FakeTimeProvider _clock = new FakeTimeProvider { AutoAdvanceAmount = TimeSpan.FromSeconds(1) };

        public SearchImageStepWorkerTests()
        {
            _resolver.Areas[7] = AreaResolution.Ok(Bounds);
        }

        private static FlowStepTemplate Template(int id, string name, float accuracy, bool isRequired = false)
        {
            return new FlowStepTemplate { Id = id, Name = name, Accuracy = accuracy, IsRequired = isRequired, TemplateImage = [(byte)id] };
        }

        private static FlowStep ImageSearch(SearchModeEnum mode, params FlowStepTemplate[] templates)
        {
            return new FlowStep { Id = 1, Name = "Find play", FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, SearchMode = mode, FlowAreaId = 7, MaxMatches = 10, FlowStepTemplates = templates.ToList() };
        }

        private async Task<(ExecutionStep Result, ExecutionCacheService Cache)> SearchImage(FlowStep step)
        {
            ExecutionCacheService cache = await WorkerCache.ForAsync(step);
            SearchImageStepWorker worker = new SearchImageStepWorker(new ImageSearcher(_screenshots, _matcher), _resolver, _clock);
            return (await worker.ExecuteAsync(step, cache, Ct), cache);
        }

        [Fact]
        public async Task A_found_template_is_clicked_on_the_screen_not_in_the_area()
        {
            _matcher.Found(1, 0.9f, x: 5, y: 5);
            FlowStepTemplate play = Template(1, "play", 0.8f);
            play.ClickOffsetX = 10;
            play.ClickOffsetY = 4;

            (ExecutionStep result, _) = await SearchImage(ImageSearch(SearchModeEnum.FIND_BEST, play));

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            result.Location.ShouldBe(new Point(115, 209));
        }

        [Fact]
        public async Task A_miss_names_the_closest_template_against_each_ones_own_accuracy()
        {
            _matcher.NotFound(1, 0.4f).NotFound(2, 0.78f);

            (ExecutionStep result, _) = await SearchImage(ImageSearch(SearchModeEnum.FIND_BEST, Template(1, "play", 0.8f), Template(2, "play hover", 0.9f)));

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message.ShouldBe("no template matched, closest play hover at 0.78 - play at 0.80, play hover at 0.90, FIND_BEST");
            result.BestScore.ShouldBe(0.78f);
            result.BestTemplateId.ShouldBe(2);
        }

        [Fact]
        public async Task A_missing_required_template_is_named_in_the_failure()
        {
            _matcher.Found(1, 0.9f).NotFound(2, 0.3f);

            (ExecutionStep result, _) = await SearchImage(ImageSearch(SearchModeEnum.FIND_BEST, Template(1, "play", 0.8f), Template(2, "banner", 0.8f, isRequired: true)));

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message!.ShouldStartWith("required banner not found");
        }

        [Fact]
        public async Task Find_all_keeps_every_hit_for_the_loop_and_answers_with_the_first()
        {
            _matcher.Answer(1, new TemplateMatchOutcome
            {
                Matches =
                [
                    new TemplateMatchResult { X = 10, Y = 10, Score = 0.9f, Scale = 1f },
                    new TemplateMatchResult { X = 50, Y = 10, Score = 0.85f, Scale = 1f },
                ],
            });

            (ExecutionStep result, ExecutionCacheService cache) = await SearchImage(ImageSearch(SearchModeEnum.FIND_ALL, Template(1, "add", 0.8f)));

            result.MatchCount.ShouldBe(2);
            result.Location.ShouldBe(new Point(110, 210));
            cache.GetMatchesFrom(1).ShouldBe([new Point(110, 210), new Point(150, 210)]);
        }

        [Fact]
        public async Task A_wait_that_times_out_reports_the_best_over_every_attempt()
        {
            _matcher.NotFound(1, 0.6f);
            FlowStep step = ImageSearch(SearchModeEnum.WAIT_UNTIL_FOUND, Template(1, "play", 0.8f));
            step.TimeoutMilliseconds = 3000;

            (ExecutionStep result, _) = await SearchImage(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message!.ShouldStartWith("gave up waiting, closest play at 0.60");
            _matcher.Requests.Count.ShouldBeGreaterThan(1);
        }

        [Fact]
        public async Task Waiting_for_something_to_go_succeeds_at_once_when_it_is_not_there()
        {
            (ExecutionStep result, _) = await SearchImage(ImageSearch(SearchModeEnum.WAIT_UNTIL_NOT_FOUND, Template(1, "spinner", 0.8f)));

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            _matcher.Requests.Count.ShouldBe(1);
        }

        [Fact]
        public async Task An_area_that_cannot_be_found_fails_with_its_reason()
        {
            _resolver.Areas[7] = AreaResolution.Fail(@"No window matches ""Browser"".");

            (ExecutionStep result, _) = await SearchImage(ImageSearch(SearchModeEnum.FIND_BEST, Template(1, "play", 0.8f)));

            result.Message.ShouldBe(@"No window matches ""Browser"".");
            _screenshots.Captured.ShouldBeEmpty();
        }
    }
}
