using Business.Executions;
using Business.FlowScript;
using Business.FlowScript.Text;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Business.Git;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions
{
    /// <summary>
    /// What an execution leaves behind: the version it executed, its steps in batches, and its last
    /// screenshots, written as it ends.
    /// </summary>
    public sealed class ExecutionHistoryServiceTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly FakeGitRepositoryService _repository = new FakeGitRepositoryService();
        private readonly FakeAppSettingService _settings = new FakeAppSettingService();
        private readonly FakeTimeProvider _time = new FakeTimeProvider();
        private readonly string _historyFolder = Path.Combine(Path.GetTempPath(), "stepinflow-history-" + Guid.NewGuid().ToString("N")[..8]);

        public void Dispose()
        {
            _database.Dispose();

            if (Directory.Exists(_historyFolder))
                Directory.Delete(_historyFolder, recursive: true);
        }

        private ExecutionHistoryService History()
        {
            FlowScriptExporter exporter = new FlowScriptExporter(_database, new Printer());

            return new ExecutionHistoryService(_database, _settings, _repository, exporter, _time, NullLogger<ExecutionHistoryService>.Instance, _historyFolder);
        }

        private async Task<int> StartAsync(ExecutionHistoryService history, ExecutionHistoryLevelEnum historyLevel)
        {
            await using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = "Login", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            await db.SaveChangesAsync(Ct);

            ExecutionStartDto dto = new ExecutionStartDto { FlowId = flow.Id, HistoryLevel = historyLevel };

            return await history.StartAsync(dto, new Dictionary<int, FlowStep>(), Ct);
        }

        private async Task<Execution> StartAsync()
        {
            int executionId = await StartAsync(History(), ExecutionHistoryLevelEnum.STEPS);

            await using AppDbContext db = _database.CreateDbContext();
            return await db.Executions.AsNoTracking().SingleAsync(x => x.Id == executionId, Ct);
        }

        private async Task<List<ExecutionStep>> StepsAsync(int executionId)
        {
            await using AppDbContext db = _database.CreateDbContext();

            return await db.ExecutionSteps.AsNoTracking().Where(x => x.ExecutionId == executionId).OrderBy(x => x.Sequence).ToListAsync(Ct);
        }

        private async Task<string> FolderOfAsync(int executionId)
        {
            await using AppDbContext db = _database.CreateDbContext();

            string? folderName = await db.Executions.Where(x => x.Id == executionId).Select(x => x.ScreenshotFolderName).SingleAsync(Ct);

            return Path.Combine(_historyFolder, folderName.ShouldNotBeNull());
        }

        private static ExecutionStep Search(int sequence, ExecutionScreenshot? screenshot, StepOutcomeEnum outcome = StepOutcomeEnum.FAILURE)
        {
            return new ExecutionStep
            {
                Sequence = sequence,
                Name = "Find the button",
                FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE,
                Outcome = outcome,
                Screenshot = screenshot,
            };
        }

        private static ExecutionScreenshot Screenshot(byte value)
        {
            return new ExecutionScreenshot([value, value, value], "Find the button", DateTime.UnixEpoch);
        }


        // The version.

        [Fact]
        public async Task A_flow_executed_as_committed_records_the_commit_and_no_script()
        {
            Execution execution = await StartAsync();

            (execution.CommitSha, execution.HasUncommittedChanges).ShouldBe((new string('a', 40), false));
            execution.Script.ShouldBeNull();
        }

        [Fact]
        public async Task A_flow_executed_with_uncommitted_changes_keeps_its_script()
        {
            _repository.Version = new GitFlowVersion { CommitSha = new string('a', 40), HasUncommittedChanges = true };

            Execution execution = await StartAsync();

            execution.HasUncommittedChanges.ShouldBe(true);
            execution.Script.ShouldNotBeNull().ShouldStartWith("Script Language: v1");
        }

        // The record is lost, never the execution.
        [Fact]
        public async Task A_repository_that_cannot_be_read_still_starts_and_keeps_the_script()
        {
            _repository.Version = null;

            Execution execution = await StartAsync();

            (execution.CommitSha, execution.HasUncommittedChanges).ShouldBe((null, null));
            execution.Script.ShouldNotBeNull().ShouldStartWith("Script Language: v1");
        }


        // The steps.

        // A search that finds nothing is most of a polling loop, so it is no reason to write.
        [Fact]
        public async Task A_failed_step_waits_for_its_batch_like_any_other()
        {
            ExecutionHistoryService history = History();
            int executionId = await StartAsync(history, ExecutionHistoryLevelEnum.STEPS);

            await history.RecordAsync(Search(0, null));
            (await StepsAsync(executionId)).ShouldBeEmpty();

            _time.Advance(TimeSpan.FromSeconds(1));
            await history.RecordAsync(Search(1, null, StepOutcomeEnum.SUCCESS));

            (await StepsAsync(executionId)).Select(x => x.Sequence).ShouldBe([0, 1]);
        }

        [Fact]
        public async Task A_flush_writes_the_steps_still_waiting()
        {
            ExecutionHistoryService history = History();
            int executionId = await StartAsync(history, ExecutionHistoryLevelEnum.STEPS);

            await history.RecordAsync(Search(0, null));
            await history.FlushAsync();

            (await StepsAsync(executionId)).Count.ShouldBe(1);
        }


        // The screenshots.

        [Fact]
        public async Task An_execution_keeps_its_last_screenshots_and_writes_them_as_it_ends()
        {
            _settings.Stored[AppSettingKeyEnum.EXECUTION_SCREENSHOT_LIMIT] = "3";
            ExecutionHistoryService history = History();
            int executionId = await StartAsync(history, ExecutionHistoryLevelEnum.STEPS_AND_IMAGES);

            for (int i = 0; i < 5; i++)
                await history.RecordAsync(Search(i, Screenshot((byte)i)));

            Directory.Exists(_historyFolder).ShouldBeFalse();

            await history.CompleteAsync(ExecutionStatusEnum.STOPPED, string.Empty, null, 5);

            List<ExecutionStep> steps = await StepsAsync(executionId);
            steps.Where(x => x.ScreenshotFileName != null).Select(x => x.Sequence).ShouldBe([2, 3, 4]);

            string folder = await FolderOfAsync(executionId);
            Directory.GetFiles(folder).Select(Path.GetFileName).Order().ShouldBe(["2 Find the button.jpg", "3 Find the button.jpg", "4 Find the button.jpg"]);
            File.ReadAllBytes(Path.Combine(folder, "4 Find the button.jpg")).ShouldBe([4, 4, 4]);
        }

        [Fact]
        public async Task The_hits_of_a_find_all_share_the_file_of_the_search_they_came_off()
        {
            ExecutionHistoryService history = History();
            int executionId = await StartAsync(history, ExecutionHistoryLevelEnum.STEPS_AND_IMAGES);

            ExecutionScreenshot screenshot = Screenshot(7);
            for (int i = 0; i < 3; i++)
                await history.RecordAsync(Search(i, screenshot, StepOutcomeEnum.SUCCESS));

            await history.CompleteAsync(ExecutionStatusEnum.COMPLETED, string.Empty, null, 3);

            (await StepsAsync(executionId)).Select(x => x.ScreenshotFileName).ShouldBe(["0 Find the button.jpg", "0 Find the button.jpg", "0 Find the button.jpg"]);
            Directory.GetFiles(await FolderOfAsync(executionId)).Length.ShouldBe(1);
        }

        // The page asks while the execution is going: from memory, since nothing is on disk yet.
        [Fact]
        public async Task A_kept_screenshot_is_shown_from_memory_until_the_execution_ends()
        {
            _settings.Stored[AppSettingKeyEnum.EXECUTION_SCREENSHOT_LIMIT] = "1";
            ExecutionHistoryService history = History();
            int executionId = await StartAsync(history, ExecutionHistoryLevelEnum.STEPS_AND_IMAGES);

            ExecutionStep first = Search(0, Screenshot(1));
            ExecutionStep second = Search(1, Screenshot(2));
            await history.RecordAsync(first);
            await history.RecordAsync(second);
            await history.FlushAsync();

            history.GetKeptScreenshot(first.Id).ShouldBeNull();
            first.Screenshot.ShouldBeNull();
            history.GetKeptScreenshot(second.Id).ShouldBe([2, 2, 2]);

            await history.CompleteAsync(ExecutionStatusEnum.STOPPED, string.Empty, null, 2);

            history.GetKeptScreenshot(second.Id).ShouldBeNull();
            (await StepsAsync(executionId)).Select(x => x.ScreenshotFileName).ShouldBe([null, "1 Find the button.jpg"]);
        }

        [Fact]
        public async Task An_execution_that_keeps_steps_only_writes_no_screenshot()
        {
            ExecutionHistoryService history = History();
            int executionId = await StartAsync(history, ExecutionHistoryLevelEnum.STEPS);

            ExecutionStep step = Search(0, Screenshot(1));
            await history.RecordAsync(step);
            await history.CompleteAsync(ExecutionStatusEnum.COMPLETED, string.Empty, null, 1);

            step.Screenshot.ShouldBeNull();
            (await StepsAsync(executionId)).Single().ScreenshotFileName.ShouldBeNull();
            Directory.Exists(_historyFolder).ShouldBeFalse();
        }
    }
}
