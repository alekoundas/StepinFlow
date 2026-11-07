using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Transport.Ipc.Handlers.Execution;
using Transport.Tests.Fakes;

namespace Transport.Tests.Execution
{
    public sealed class GetExecutionStatsHandlerTests : IDisposable
    {
        private static readonly DateTime NOW = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        private static DateTime DaysAgo(double days)
        {
            return NOW.AddDays(-days);
        }

        private async Task<ExecutionStatsDto> StatsAsync(ExecutionStatsRequestDto dto)
        {
            ResultDto<ExecutionStatsDto> result = await new GetExecutionStatsHandler(_database, new FixedTimeProvider()).HandleAsync(dto, Ct);
            return result.Data!;
        }

        private int SeedFlow(string name, bool isHidden = false)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = name, IsHidden = isHidden };
            db.Flows.Add(flow);
            db.SaveChanges();

            return flow.Id;
        }

        private void SeedExecution(int flowId, ExecutionStatusEnum status, DateTime createdOn, double? tookSeconds = null)
        {
            using AppDbContext db = _database.CreateDbContext();

            Core.Models.Database.Execution execution = new Core.Models.Database.Execution { FlowId = flowId, Status = status, CreatedOn = createdOn };
            if (tookSeconds != null)
                execution.CompletedAt = createdOn.AddSeconds(tookSeconds.Value);

            db.Executions.Add(execution);
            db.SaveChanges();
        }

        [Fact]
        public async Task Executions_are_counted_by_how_they_ended()
        {
            int flowId = SeedFlow("Login");
            SeedExecution(flowId, ExecutionStatusEnum.COMPLETED, DaysAgo(1), 10);
            SeedExecution(flowId, ExecutionStatusEnum.COMPLETED, DaysAgo(1), 20);
            SeedExecution(flowId, ExecutionStatusEnum.FAILED, DaysAgo(1));
            SeedExecution(flowId, ExecutionStatusEnum.ERRORED, DaysAgo(1));
            SeedExecution(flowId, ExecutionStatusEnum.STOPPED, DaysAgo(1));
            SeedExecution(flowId, ExecutionStatusEnum.ABANDONED, DaysAgo(1));
            SeedExecution(flowId, ExecutionStatusEnum.INCONCLUSIVE, DaysAgo(1));
            SeedExecution(flowId, ExecutionStatusEnum.RUNNING, DaysAgo(0));

            ExecutionStatsDto stats = await StatsAsync(new ExecutionStatsRequestDto());

            stats.Total.ShouldBe(8);
            stats.Completed.ShouldBe(2);
            stats.Failed.ShouldBe(1);
            stats.Errored.ShouldBe(1);
            stats.Stopped.ShouldBe(1);
            stats.Abandoned.ShouldBe(1);
            stats.Inconclusive.ShouldBe(1);
            stats.Executing.ShouldBe(1);
        }

        [Fact]
        public async Task The_last_days_leave_older_executions_out()
        {
            int flowId = SeedFlow("Login");
            SeedExecution(flowId, ExecutionStatusEnum.COMPLETED, DaysAgo(2), 10);
            SeedExecution(flowId, ExecutionStatusEnum.FAILED, DaysAgo(10));

            ExecutionStatsDto stats = await StatsAsync(new ExecutionStatsRequestDto { Days = 7 });

            stats.Total.ShouldBe(1);
            stats.Failed.ShouldBe(0);
            stats.LastFailureOn.ShouldBeNull();
        }

        [Fact]
        public async Task One_flow_and_a_hidden_one_are_kept_apart()
        {
            int login = SeedFlow("Login");
            int price = SeedFlow("Price");
            int hidden = SeedFlow("Gone", true);
            SeedExecution(login, ExecutionStatusEnum.COMPLETED, DaysAgo(1), 10);
            SeedExecution(price, ExecutionStatusEnum.COMPLETED, DaysAgo(1), 10);
            SeedExecution(hidden, ExecutionStatusEnum.COMPLETED, DaysAgo(1), 10);

            (await StatsAsync(new ExecutionStatsRequestDto { FlowId = login })).Total.ShouldBe(1);
            (await StatsAsync(new ExecutionStatsRequestDto())).Total.ShouldBe(2);
        }

        [Fact]
        public async Task The_usual_time_is_the_median_of_the_completed_ones()
        {
            int flowId = SeedFlow("Login");
            SeedExecution(flowId, ExecutionStatusEnum.COMPLETED, DaysAgo(1), 10);
            SeedExecution(flowId, ExecutionStatusEnum.COMPLETED, DaysAgo(1), 12);
            SeedExecution(flowId, ExecutionStatusEnum.COMPLETED, DaysAgo(1), 3600);
            SeedExecution(flowId, ExecutionStatusEnum.STOPPED, DaysAgo(1), 50000);

            ExecutionStatsDto stats = await StatsAsync(new ExecutionStatsRequestDto());

            stats.UsualDurationMilliseconds.ShouldBe(12000);
        }

        [Fact]
        public async Task The_last_failure_is_the_newest_failed_or_errored_one()
        {
            int flowId = SeedFlow("Login");
            DateTime older = DaysAgo(3);
            DateTime newer = DaysAgo(2);
            SeedExecution(flowId, ExecutionStatusEnum.FAILED, older);
            SeedExecution(flowId, ExecutionStatusEnum.ERRORED, newer);
            SeedExecution(flowId, ExecutionStatusEnum.COMPLETED, DaysAgo(1), 10);

            ExecutionStatsDto stats = await StatsAsync(new ExecutionStatsRequestDto());

            stats.LastFailureOn.ShouldBe(newer);
            stats.UsualDurationMilliseconds.ShouldBe(10000);
        }

        [Fact]
        public async Task Nothing_executed_counts_nothing()
        {
            ExecutionStatsDto stats = await StatsAsync(new ExecutionStatsRequestDto());

            stats.Total.ShouldBe(0);
            stats.LastFailureOn.ShouldBeNull();
            stats.UsualDurationMilliseconds.ShouldBeNull();
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed class FixedTimeProvider : TimeProvider
        {
            public override DateTimeOffset GetUtcNow()
            {
                return new DateTimeOffset(NOW);
            }
        }
    }
}
