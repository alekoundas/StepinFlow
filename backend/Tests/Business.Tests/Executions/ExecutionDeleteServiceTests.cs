using Business.Executions;
using Business.Flows.DataService;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions
{
    /// <summary>
    /// Deleting executions, one or a flow's, and a flow with its executions: the rows go, the
    /// screenshots folders go, and the one executing now stays.
    /// </summary>
    public sealed class ExecutionDeleteServiceTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly FakeExecutionEngine _engine = new FakeExecutionEngine();

        // The history folder sits inside one of the test's own, so a name reaching outside it
        // reaches nothing else.
        private readonly string _sandbox = Path.Combine(Path.GetTempPath(), "stepinflow-delete-" + Guid.NewGuid().ToString("N")[..8]);
        private readonly string _historyFolder;

        public ExecutionDeleteServiceTests()
        {
            _historyFolder = Path.Combine(_sandbox, "Execution History");
        }

        public void Dispose()
        {
            _database.Dispose();

            if (Directory.Exists(_sandbox))
                Directory.Delete(_sandbox, recursive: true);
        }

        private ExecutionDeleteService Deletes()
        {
            return new ExecutionDeleteService(_database, _engine, NullLogger<ExecutionDeleteService>.Instance, _historyFolder);
        }

        private int SeedFlow(string name)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = name, PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            return flow.Id;
        }

        // An execution of three steps, its screenshot on disk in a folder of its own.
        private int SeedExecution(int flowId, string folderName)
        {
            using AppDbContext db = _database.CreateDbContext();

            Execution execution = new Execution { FlowId = flowId, Status = ExecutionStatusEnum.STOPPED, ScreenshotFolderName = folderName };
            db.Executions.Add(execution);
            db.SaveChanges();

            for (int i = 0; i < 3; i++)
                db.ExecutionSteps.Add(new ExecutionStep { ExecutionId = execution.Id, Sequence = i, Name = "Find the button", ScreenshotFileName = "0 Find the button.jpg" });
            db.SaveChanges();

            string folder = Path.Combine(_historyFolder, folderName);
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "0 Find the button.jpg"), [1, 2, 3]);

            return execution.Id;
        }

        private List<int> ExecutionIds()
        {
            using AppDbContext db = _database.CreateDbContext();

            return db.Executions.OrderBy(x => x.Id).Select(x => x.Id).ToList();
        }

        private List<int> StepExecutionIds()
        {
            using AppDbContext db = _database.CreateDbContext();

            return db.ExecutionSteps.Select(x => x.ExecutionId).Distinct().Order().ToList();
        }


        [Fact]
        public async Task Deleting_an_execution_takes_its_steps_and_its_screenshots_folder()
        {
            int flowId = SeedFlow("Firestone");
            int deleted = SeedExecution(flowId, "Firestone - 1");
            int kept = SeedExecution(flowId, "Firestone - 2");

            ResultDto<bool> result = await Deletes().DeleteAsync(deleted, Ct);

            result.IsSuccess.ShouldBeTrue();
            ExecutionIds().ShouldBe([kept]);
            StepExecutionIds().ShouldBe([kept]);
            Directory.Exists(Path.Combine(_historyFolder, "Firestone - 1")).ShouldBeFalse();
            Directory.Exists(Path.Combine(_historyFolder, "Firestone - 2")).ShouldBeTrue();
        }

        [Fact]
        public async Task The_execution_going_now_is_not_deleted()
        {
            int executionId = SeedExecution(SeedFlow("Firestone"), "Firestone - 1");
            _engine.IsRunning = true;
            _engine.ExecutionId = executionId;

            ResultDto<bool> result = await Deletes().DeleteAsync(executionId, Ct);

            result.IsSuccess.ShouldBeFalse();
            ExecutionIds().ShouldBe([executionId]);
            Directory.Exists(Path.Combine(_historyFolder, "Firestone - 1")).ShouldBeTrue();
        }

        [Fact]
        public async Task Deleting_a_flows_executions_leaves_the_one_going_now_and_other_flows()
        {
            int flowId = SeedFlow("Firestone");
            SeedExecution(flowId, "Firestone - 1");
            int live = SeedExecution(flowId, "Firestone - 2");
            int other = SeedExecution(SeedFlow("Login"), "Login - 1");
            _engine.IsRunning = true;
            _engine.ExecutionId = live;

            ResultDto<int> result = await Deletes().DeleteForFlowAsync(flowId, Ct);

            result.Data.ShouldBe(1);
            ExecutionIds().ShouldBe([live, other]);
            Directory.Exists(Path.Combine(_historyFolder, "Firestone - 1")).ShouldBeFalse();
        }

        // The name comes from the database, and a name that would reach outside is left alone.
        [Fact]
        public async Task A_folder_name_that_reaches_outside_the_history_is_not_deleted()
        {
            int executionId = SeedExecution(SeedFlow("Firestone"), "Firestone - 1");
            using (AppDbContext db = _database.CreateDbContext())
            {
                db.Executions.Where(x => x.Id == executionId).ExecuteUpdate(x => x.SetProperty(execution => execution.ScreenshotFolderName, ".."));
            }

            ResultDto<bool> result = await Deletes().DeleteAsync(executionId, Ct);

            result.IsSuccess.ShouldBeTrue();
            Directory.Exists(Path.Combine(_historyFolder, "Firestone - 1")).ShouldBeTrue();
        }

        [Fact]
        public async Task Deleting_a_flow_takes_its_executions_and_their_folders()
        {
            int flowId = SeedFlow("Firestone");
            SeedExecution(flowId, "Firestone - 1");
            DataService dataService = TestDataService.For(_database, executionEngine: _engine, historyFolder: _historyFolder);

            ResultDto<bool> result = await dataService.Flow.DeleteAsync(flowId, Ct);

            result.IsSuccess.ShouldBeTrue();
            ExecutionIds().ShouldBeEmpty();
            StepExecutionIds().ShouldBeEmpty();
            Directory.Exists(Path.Combine(_historyFolder, "Firestone - 1")).ShouldBeFalse();
        }

        [Fact]
        public async Task A_flow_an_execution_is_using_is_not_deleted()
        {
            int flowId = SeedFlow("Claim rewards");
            SeedExecution(flowId, "Claim rewards - 1");
            _engine.IsRunning = true;
            _engine.ExecutingFlowIds.Add(flowId);
            DataService dataService = TestDataService.For(_database, executionEngine: _engine, historyFolder: _historyFolder);

            ResultDto<bool> result = await dataService.Flow.DeleteAsync(flowId, Ct);

            result.IsSuccess.ShouldBeFalse();
            ExecutionIds().Count.ShouldBe(1);
            Directory.Exists(Path.Combine(_historyFolder, "Claim rewards - 1")).ShouldBeTrue();
        }
    }
}
