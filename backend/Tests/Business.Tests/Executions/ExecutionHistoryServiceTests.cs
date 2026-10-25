using Business.Executions;
using Business.FlowScript;
using Business.FlowScript.Text;
using Business.Tests.Fakes;
using Core.Models.Business.Git;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions
{
    /// <summary>
    /// What an execution records of the version it executed: the commit, whether anything was left
    /// uncommitted, and the script when the commit alone cannot say.
    /// </summary>
    public sealed class ExecutionHistoryServiceTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly FakeGitRepositoryService _repository = new FakeGitRepositoryService();

        public void Dispose()
        {
            _database.Dispose();
        }

        private ExecutionHistoryService History()
        {
            FakeAppSettingService settings = new FakeAppSettingService();
            ExecutionCacheService cache = new ExecutionCacheService(settings, new FakeScreenshotService(), TimeProvider.System);
            FlowScriptExporter exporter = new FlowScriptExporter(_database, new Printer());

            return new ExecutionHistoryService(_database, cache, settings, _repository, exporter, TimeProvider.System, NullLogger<ExecutionHistoryService>.Instance);
        }

        private int SeedFlow()
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = "Login", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            return flow.Id;
        }

        private async Task<Execution> StartAsync()
        {
            int flowId = SeedFlow();
            ExecutionStartDto dto = new ExecutionStartDto { FlowId = flowId };

            int executionId = await History().StartAsync(dto, new Dictionary<int, FlowStep>(), Ct);

            await using AppDbContext db = _database.CreateDbContext();
            return await db.Executions.AsNoTracking().SingleAsync(x => x.Id == executionId, Ct);
        }


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
            execution.Script.ShouldNotBeNull().ShouldStartWith("Flow:");
        }

        // The record is lost, never the execution.
        [Fact]
        public async Task A_repository_that_cannot_be_read_still_starts_and_keeps_the_script()
        {
            _repository.Version = null;

            Execution execution = await StartAsync();

            (execution.CommitSha, execution.HasUncommittedChanges).ShouldBe((null, null));
            execution.Script.ShouldNotBeNull().ShouldStartWith("Flow:");
        }
    }
}
