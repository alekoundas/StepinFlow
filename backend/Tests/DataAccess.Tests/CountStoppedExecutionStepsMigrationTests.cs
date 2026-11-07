using Core.Models.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    public sealed class CountStoppedExecutionStepsMigrationTests : IDisposable
    {
        private const string BEFORE = "20261107040041_DropExecutionViewport";
        private const string AFTER = "20261107134007_CountStoppedExecutionSteps";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public void A_stopped_or_abandoned_execution_counts_the_steps_its_history_kept()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int stopped = SavedExecution(flowId, "STOPPED", 0, 3);
            int abandoned = SavedExecution(flowId, "ABANDONED", 0, 2);
            MigrateTo(AFTER);

            StepCountOf(stopped).ShouldBe(3);
            StepCountOf(abandoned).ShouldBe(2);
        }

        [Fact]
        public void A_count_already_written_is_left_as_it_was()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int completed = SavedExecution(flowId, "COMPLETED", 5, 2);
            int stoppedWithCount = SavedExecution(flowId, "STOPPED", 4, 1);
            MigrateTo(AFTER);

            StepCountOf(completed).ShouldBe(5);
            StepCountOf(stoppedWithCount).ShouldBe(4);
        }

        [Fact]
        public void One_that_kept_no_history_stays_at_0()
        {
            MigrateTo(BEFORE);
            int stopped = SavedExecution(SavedFlow(), "STOPPED", 0, 0);
            MigrateTo(AFTER);

            StepCountOf(stopped).ShouldBe(0);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        private int StepCountOf(int executionId)
        {
            using AppDbContext db = _database.Open();
            return db.Executions.Where(x => x.Id == executionId).Select(x => x.StepCount).Single();
        }

        private int SavedFlow()
        {
            using AppDbContext db = _database.Open();

            Flow flow = new Flow { Name = "Looping", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            return flow.Id;
        }

        private int SavedExecution(int flowId, string status, int stepCount, int keptSteps)
        {
            using AppDbContext db = _database.Open();

            db.Database.ExecuteSqlRaw(@"
                INSERT INTO Executions (Status, HistoryLevel, StepCount, CsvRowIndex, ErrorMessage, FlowStructureHash, FlowId, CreatedOn)
                VALUES ({0}, 'STEPS', {1}, 0, '', 'hash', {2}, '2026-11-07');", status, stepCount, flowId);

            int id = db.Executions.Max(x => x.Id);

            for (int sequence = 0; sequence < keptSteps; sequence++)
            {
                db.Database.ExecuteSqlRaw(@"
                    INSERT INTO ExecutionSteps (Sequence, Depth, Name, FlowStepType, Outcome, StartedOn, DurationMilliseconds, ExecutionId, CreatedOn)
                    VALUES ({0}, 0, 'Wait', 'WAIT', 'SUCCESS', '2026-11-07', 10, {1}, '2026-11-07');", sequence, id);
            }

            return id;
        }
    }
}
