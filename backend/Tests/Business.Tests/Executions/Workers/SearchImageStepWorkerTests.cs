using System.Drawing;

using Business.Executions;
using Business.Executions.Workers;
using Business.Searching;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Business.OpenCV;
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

        private static FlowStepTemplate Template(int id, string name, float accuracy)
        {
            return new FlowStepTemplate { Id = id, Accuracy = accuracy, FlowTemplate = new FlowTemplate { Id = id, Name = name, TemplateImage = [(byte)id] } };
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
            play.FlowTemplate.ClickOffsetX = 10;
            play.FlowTemplate.ClickOffsetY = 4;

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

        // The shape was there in another state, which lowering the accuracy would not mend.
        [Fact]
        public async Task A_miss_says_which_template_had_the_shape_and_what_refused_it()
        {
            _matcher.Answer(1, RefusedFor(MatchRefusalEnum.COLOUR, nearMiss: 0.4f)).NotFound(2, 0.62f);

            (ExecutionStep result, _) = await SearchImage(ImageSearch(SearchModeEnum.FIND_BEST, Template(1, "research", 0.8f), Template(2, "research icon", 0.8f)));

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message.ShouldBe("no template matched, closest research icon at 0.62, research at 0.97 refused by its colour, 41 apart where 17 is allowed - research at 0.80, research icon at 0.80, FIND_BEST");
            result.BestScore.ShouldBe(0.62f);
        }

        [Fact]
        public async Task A_contrast_refusal_says_the_contrast_and_what_is_allowed()
        {
            _matcher.Answer(1, RefusedFor(MatchRefusalEnum.CONTRAST, nearMiss: 0.4f));

            (ExecutionStep result, _) = await SearchImage(ImageSearch(SearchModeEnum.FIND_BEST, Template(1, "claim", 0.8f)));

            result.Message.ShouldBe("no template matched, closest claim at 0.40, claim at 0.97 refused by its contrast, 0.70 where 0.85 to 1.18 is allowed - claim at 0.80, FIND_BEST");
        }

        // Gone as it was: the shape is still there, greyed, and the message says so.
        [Fact]
        public async Task Waiting_for_something_to_go_says_what_is_left_of_it()
        {
            _matcher.Answer(1, RefusedFor(MatchRefusalEnum.COLOUR, nearMiss: 0.4f));

            (ExecutionStep result, _) = await SearchImage(ImageSearch(SearchModeEnum.WAIT_UNTIL_NOT_FOUND, Template(1, "start", 0.8f)));

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            result.Message.ShouldBe("No longer on screen as it was: start at 0.97 refused by its colour, 41 apart where 17 is allowed.");
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

        // The template is too big for the window as it is now, which a bigger window mends.
        [Theory]
        [InlineData(SearchModeEnum.WAIT_UNTIL_NOT_FOUND)]
        [InlineData(SearchModeEnum.WAIT_UNTIL_FOUND)]
        [InlineData(SearchModeEnum.WAIT_UNTIL_STILL)]
        public async Task A_search_that_cannot_be_done_is_tried_again_and_never_counts_as_gone(SearchModeEnum mode)
        {
            _matcher.Answer(1, new TemplateMatchOutcome { Error = "scaled by 4.00 it is bigger than the area." });
            FlowStep step = ImageSearch(mode, Template(1, "spinner", 0.8f));
            step.TimeoutMilliseconds = 3000;

            (ExecutionStep result, _) = await SearchImage(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message!.ShouldEndWith("The last try could not look: Template 1: scaled by 4.00 it is bigger than the area.");
            _matcher.Requests.Count.ShouldBeGreaterThan(1);
        }

        [Fact]
        public async Task An_area_that_cannot_be_found_fails_with_its_reason()
        {
            _resolver.Areas[7] = AreaResolution.Fail(@"No window matches ""Browser"".", AreaUnresolvedEnum.NOT_ON_SCREEN);

            (ExecutionStep result, _) = await SearchImage(ImageSearch(SearchModeEnum.FIND_BEST, Template(1, "play", 0.8f)));

            result.Message.ShouldBe(@"No window matches ""Browser"".");
            _screenshots.Captured.ShouldBeEmpty();
        }

        // ================================================================
        // The window, found again on every try of a wait
        // ================================================================

        [Fact]
        public async Task A_wait_finds_a_window_that_opens_while_it_waits()
        {
            _matcher.Found(1, 0.9f, x: 5, y: 5);
            _resolver.AreasInTurn[7] = new Queue<AreaResolution>([NoWindow(), NoWindow(), AreaResolution.Ok(Bounds)]);
            FlowStep step = ImageSearch(SearchModeEnum.WAIT_UNTIL_FOUND, Template(1, "play", 0.8f));
            step.TimeoutMilliseconds = 0;

            (ExecutionStep result, _) = await SearchImage(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            result.Location.ShouldBe(new Point(105, 205));
            _resolver.AreaLookups.ShouldBe(3);
        }

        [Fact]
        public async Task A_wait_looks_where_the_window_is_now()
        {
            Rectangle moved = new Rectangle(300, 400, 800, 600);
            _matcher.NotFound(1, 0.4f);
            _resolver.AreasInTurn[7] = new Queue<AreaResolution>([AreaResolution.Ok(Bounds), AreaResolution.Ok(moved)]);
            FlowStep step = ImageSearch(SearchModeEnum.WAIT_UNTIL_FOUND, Template(1, "play", 0.8f));
            step.TimeoutMilliseconds = 3000;

            await SearchImage(step);

            _screenshots.Captured.Take(2).ShouldBe([Bounds, moved]);
        }

        [Fact]
        public async Task A_window_that_never_opens_is_named_when_the_wait_gives_up()
        {
            _resolver.Areas[7] = NoWindow();
            FlowStep step = ImageSearch(SearchModeEnum.WAIT_UNTIL_FOUND, Template(1, "play", 0.8f));
            step.TimeoutMilliseconds = 3000;

            (ExecutionStep result, _) = await SearchImage(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message!.ShouldEndWith(@"The last try could not look: No window matches ""Browser"".");
        }

        // What it waits to see go was in the window, so it went with it.
        [Fact]
        public async Task Waiting_for_something_to_go_succeeds_when_its_window_closes()
        {
            _matcher.Found(1, 0.9f);
            _resolver.AreasInTurn[7] = new Queue<AreaResolution>([AreaResolution.Ok(Bounds), NoWindow()]);
            FlowStep step = ImageSearch(SearchModeEnum.WAIT_UNTIL_NOT_FOUND, Template(1, "spinner", 0.8f));
            step.TimeoutMilliseconds = 0;

            (ExecutionStep result, _) = await SearchImage(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            result.Message.ShouldBe(@"No longer on screen: No window matches ""Browser"".");
        }

        // A minimised window comes back, with whatever was on it.
        [Fact]
        public async Task A_minimised_window_is_waited_out_rather_than_taken_as_gone()
        {
            _resolver.Areas[7] = AreaResolution.Fail(@"""Browser"" was found but has no visible area.", AreaUnresolvedEnum.NO_SIZE);
            FlowStep step = ImageSearch(SearchModeEnum.WAIT_UNTIL_NOT_FOUND, Template(1, "spinner", 0.8f));
            step.TimeoutMilliseconds = 3000;

            (ExecutionStep result, _) = await SearchImage(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            _resolver.AreaLookups.ShouldBeGreaterThan(1);
        }

        [Theory]
        [InlineData(SearchModeEnum.WAIT_UNTIL_FOUND)]
        [InlineData(SearchModeEnum.WAIT_UNTIL_STILL)]
        [InlineData(SearchModeEnum.WAIT_UNTIL_NOT_FOUND)]
        public async Task An_area_no_try_can_find_fails_a_wait_at_once(SearchModeEnum mode)
        {
            _resolver.Areas.Remove(7);
            FlowStep step = ImageSearch(mode, Template(1, "play", 0.8f));
            step.TimeoutMilliseconds = 0;

            (ExecutionStep result, _) = await SearchImage(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message.ShouldBe("The area no longer exists.");
            _resolver.AreaLookups.ShouldBe(1);
        }

        // ================================================================
        // Waiting until what it found stops moving
        // ================================================================

        [Fact]
        public async Task A_template_standing_still_counts_on_the_second_look()
        {
            _matcher.Found(1, 0.9f, x: 5, y: 5);

            (ExecutionStep result, _) = await SearchImage(ImageSearch(SearchModeEnum.WAIT_UNTIL_STILL, Template(1, "play", 0.8f)));

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            result.Location.ShouldBe(new Point(105, 205));
            _matcher.Requests.Count.ShouldBe(2);
        }

        [Fact]
        public async Task A_wait_until_found_takes_the_first_match()
        {
            _matcher.Found(1, 0.9f, x: 5, y: 5);

            (ExecutionStep result, _) = await SearchImage(ImageSearch(SearchModeEnum.WAIT_UNTIL_FOUND, Template(1, "play", 0.8f)));

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            _matcher.Requests.Count.ShouldBe(1);
        }

        [Fact]
        public async Task A_moving_template_counts_where_it_stops()
        {
            _matcher.AnswerInTurn(1, FakeOpenCvService.Seen(0, 0), FakeOpenCvService.Seen(40, 0), FakeOpenCvService.Seen(80, 0), FakeOpenCvService.Seen(80, 0));
            FlowStep step = ImageSearch(SearchModeEnum.WAIT_UNTIL_STILL, Template(1, "play", 0.8f));
            step.TimeoutMilliseconds = 0;

            (ExecutionStep result, _) = await SearchImage(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            result.Location.ShouldBe(new Point(180, 200));
            _matcher.Requests.Count.ShouldBe(4);
        }

        [Theory]
        [InlineData(2, 1f, true)]
        [InlineData(3, 1f, false)]
        [InlineData(4, 2f, true)]
        [InlineData(5, 2f, false)]
        public async Task A_pixel_or_two_of_jitter_is_still_and_grows_with_the_scale(int moved, float scale, bool isStill)
        {
            _matcher.AnswerInTurn(1, FakeOpenCvService.Seen(10, 10, scale), FakeOpenCvService.Seen(10 + moved, 10, scale), FakeOpenCvService.Seen(10 + moved, 10, scale));
            FlowStep step = ImageSearch(SearchModeEnum.WAIT_UNTIL_STILL, Template(1, "play", 0.8f));
            step.TimeoutMilliseconds = 0;

            (ExecutionStep result, _) = await SearchImage(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            _matcher.Requests.Count.ShouldBe(isStill ? 2 : 3);
        }

        [Fact]
        public async Task One_that_never_stops_fails_saying_it_was_still_moving()
        {
            _matcher.AnswerInTurn(1, [.. Enumerable.Range(0, 10).Select(x => FakeOpenCvService.Seen(x * 40, 0))]);
            FlowStep step = ImageSearch(SearchModeEnum.WAIT_UNTIL_STILL, Template(1, "play", 0.8f));
            step.TimeoutMilliseconds = 3000;

            (ExecutionStep result, _) = await SearchImage(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message.ShouldBe("found but still moving when the time ran out, last moved 40 px - play at 0.80, WAIT_UNTIL_STILL");
        }

        // Each template has its own click point, so two found in one place are not one standing still.
        [Fact]
        public async Task Another_template_in_the_same_place_is_not_the_same_one_standing_still()
        {
            _matcher.AnswerInTurn(1, FakeOpenCvService.Seen(5, 5), FakeOpenCvService.Unseen());
            _matcher.AnswerInTurn(2, FakeOpenCvService.Seen(5, 5));
            FlowStep step = ImageSearch(SearchModeEnum.WAIT_UNTIL_STILL, Template(1, "play", 0.8f), Template(2, "play hover", 0.8f));
            step.TimeoutMilliseconds = 0;

            (ExecutionStep result, _) = await SearchImage(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            _matcher.Requests.Count.ShouldBe(5);
        }

        [Fact]
        public async Task Gone_between_two_looks_starts_again()
        {
            _matcher.AnswerInTurn(1, FakeOpenCvService.Seen(5, 5), FakeOpenCvService.Unseen(), FakeOpenCvService.Seen(5, 5), FakeOpenCvService.Seen(5, 5));
            FlowStep step = ImageSearch(SearchModeEnum.WAIT_UNTIL_STILL, Template(1, "play", 0.8f));
            step.TimeoutMilliseconds = 0;

            (ExecutionStep result, _) = await SearchImage(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            _matcher.Requests.Count.ShouldBe(4);
        }

        private static AreaResolution NoWindow()
        {
            return AreaResolution.Fail(@"No window matches ""Browser"".", AreaUnresolvedEnum.NOT_ON_SCREEN);
        }

        // The shape at 0.97, its colour 41 apart where 17 is allowed or its contrast 0.70.
        private static TemplateMatchOutcome RefusedFor(MatchRefusalEnum refusedBy, float nearMiss)
        {
            return new TemplateMatchOutcome
            {
                Refused = [new TemplateMatchResult { Width = 10, Height = 10, Score = 0.97f, Scale = 1f, Colour = 41.2f, ColourLimit = 16.8f, Contrast = 0.7f, RefusedBy = refusedBy }],
                Rejected = [new TemplateMatchResult { Width = 10, Height = 10, Score = nearMiss, Scale = 1f }],
            };
        }
    }
}
