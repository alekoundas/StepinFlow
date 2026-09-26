using System.Drawing;

using Business.Executions.Workers;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;

using Microsoft.Extensions.Time.Testing;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// Capture the area, read it with OCR, narrow it with a pattern, judge it against the condition.
    /// Four things in a row, so every test carries the value it read out with the result - a check
    /// that failed without saying what the screen said is the failure this prevents.
    /// </summary>
    public sealed class SearchTextStepWorkerTests
    {
        private static readonly Rectangle Bounds = new Rectangle(100, 200, 800, 600);

        private readonly FakeScreenshotService _screenshots = new FakeScreenshotService();
        private readonly FakeOcrService _ocr = new FakeOcrService();
        private readonly FakeAreaPointResolver _resolver = new FakeAreaPointResolver();

        // Every read of the clock moves it a second, so a wait with a timeout gives up after a few
        // polls instead of after real seconds.
        private readonly FakeTimeProvider _clock = new FakeTimeProvider { AutoAdvanceAmount = TimeSpan.FromSeconds(1) };

        public SearchTextStepWorkerTests()
        {
            _resolver.Areas[7] = AreaResolution.Ok(Bounds);
        }

        private static FlowStep TextSearch(SearchModeEnum mode, ConditionTypeEnum condition, string text, string keep = "")
        {
            return new FlowStep { Id = 1, Name = "Read", FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, SearchMode = mode, FlowAreaId = 7, ConditionType = condition, ConditionText = text, ResultExtractPattern = keep, TimeoutMilliseconds = 3000 };
        }

        private async Task<ExecutionStep> SearchText(FlowStep step)
        {
            SearchTextStepWorker worker = new SearchTextStepWorker(_screenshots, _ocr, _resolver, _clock);
            return await worker.ExecuteAsync(step, await WorkerCache.ForAsync(step), Ct);
        }

        [Fact]
        public async Task Text_is_read_narrowed_and_checked()
        {
            _ocr.Reads("Total: 42 items");

            ExecutionStep result = await SearchText(TextSearch(SearchModeEnum.FIND_BEST, ConditionTypeEnum.GREATER_THAN, "40", keep: @"(\d+)"));

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            result.Value.ShouldBe("42");
            result.Location.ShouldBe(new Point(500, 500));
        }

        [Fact]
        public async Task A_failed_check_still_says_what_was_read()
        {
            _ocr.Reads("Total: 12 items");

            ExecutionStep result = await SearchText(TextSearch(SearchModeEnum.FIND_BEST, ConditionTypeEnum.GREATER_THAN, "40", keep: @"(\d+)"));

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Value.ShouldBe("12");
            result.Message.ShouldBe(@"Read ""12"", which does not satisfy GREATER_THAN 40.");
        }

        [Fact]
        public async Task A_wait_reads_again_until_the_text_says_it()
        {
            _ocr.Reads("Loading", "Loading", "Products");
            FlowStep step = TextSearch(SearchModeEnum.WAIT_UNTIL_FOUND, ConditionTypeEnum.CONTAINS, "Products");
            step.TimeoutMilliseconds = 0;

            ExecutionStep result = await SearchText(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            _ocr.ReadCount.ShouldBe(3);
        }

        [Fact]
        public async Task A_wait_that_gives_up_says_what_the_screen_said_last()
        {
            _ocr.Reads("Loading");

            ExecutionStep result = await SearchText(TextSearch(SearchModeEnum.WAIT_UNTIL_FOUND, ConditionTypeEnum.CONTAINS, "Products"));

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message.ShouldBe(@"Gave up waiting. Last read ""Loading"", against CONTAINS Products.");
        }

        [Fact]
        public async Task Waiting_for_text_to_go_succeeds_once_the_screen_stops_saying_it()
        {
            _ocr.Reads("Loading", "Loading", "Products");
            FlowStep step = TextSearch(SearchModeEnum.WAIT_UNTIL_NOT_FOUND, ConditionTypeEnum.CONTAINS, "Loading");
            step.TimeoutMilliseconds = 0;

            ExecutionStep result = await SearchText(step);

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            result.Value.ShouldBe("Products");
            _ocr.ReadCount.ShouldBe(3);
        }

        [Fact]
        public async Task Waiting_for_text_to_go_fails_when_it_is_still_there_at_the_timeout()
        {
            _ocr.Reads("Loading");

            ExecutionStep result = await SearchText(TextSearch(SearchModeEnum.WAIT_UNTIL_NOT_FOUND, ConditionTypeEnum.CONTAINS, "Loading"));

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
        }

        [Fact]
        public async Task An_area_with_no_size_is_not_read()
        {
            _resolver.Areas[7] = AreaResolution.Ok(Rectangle.Empty);

            ExecutionStep result = await SearchText(TextSearch(SearchModeEnum.FIND_BEST, ConditionTypeEnum.IS_NOT_EMPTY, string.Empty));

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            _ocr.ReadCount.ShouldBe(0);
        }
    }
}
