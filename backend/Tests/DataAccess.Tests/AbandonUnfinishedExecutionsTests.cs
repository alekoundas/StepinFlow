using Core.Enums;
using Core.Models.Database;

namespace DataAccess.Tests
{
    public sealed class AbandonUnfinishedExecutionsTests : IDisposable
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

        private int SavedExecution(ExecutionStatusEnum status, int stepCount, int keptSteps)
        {
            using AppDbContext db = _database.Open();

            Flow flow = new Flow { Name = "Looping", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            Execution execution = new Execution { FlowId = flow.Id, Status = status, StepCount = stepCount };
            db.Executions.Add(execution);
            db.SaveChanges();

            for (int sequence = 0; sequence < keptSteps; sequence++)
                db.ExecutionSteps.Add(new ExecutionStep { ExecutionId = execution.Id, Sequence = sequence, Name = "Wait", FlowStepType = FlowStepTypeEnum.WAIT });

            db.SaveChanges();
            return execution.Id;
        }

        private Execution Saved(int id)
        {
            using AppDbContext db = _database.Open();
            return db.Executions.Single(x => x.Id == id);
        }

        [Fact]
        public async Task One_still_running_is_abandoned_with_the_steps_its_history_kept()
        {
            int id = SavedExecution(ExecutionStatusEnum.RUNNING, 0, 3);

            using (AppDbContext db = _database.Open())
                await db.AbandonUnfinishedExecutionsAsync(new FixedTimeProvider());

            Execution execution = Saved(id);
            execution.Status.ShouldBe(ExecutionStatusEnum.ABANDONED);
            execution.StepCount.ShouldBe(3);
            execution.CompletedAt.ShouldBe(NOW);
        }

        [Fact]
        public async Task One_that_ended_is_left_as_it_was()
        {
            int id = SavedExecution(ExecutionStatusEnum.STOPPED, 7, 2);

            using (AppDbContext db = _database.Open())
                await db.AbandonUnfinishedExecutionsAsync(new FixedTimeProvider());

            Execution execution = Saved(id);
            execution.Status.ShouldBe(ExecutionStatusEnum.STOPPED);
            execution.StepCount.ShouldBe(7);
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
