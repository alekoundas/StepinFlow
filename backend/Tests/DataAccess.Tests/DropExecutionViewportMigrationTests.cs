using Core.Models.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    /// <summary>
    /// Dropping a column can rebuild the table, and an execution's steps cascade from it: a rebuild
    /// with foreign keys on would delete every one of them.
    /// </summary>
    public sealed class DropExecutionViewportMigrationTests : IDisposable
    {
        private const string BEFORE = "20261107031716_DropViewportsAndLastGoodScreenshots";
        private const string AFTER = "20261107040041_DropExecutionViewport";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public void An_execution_and_its_steps_outlive_the_columns()
        {
            MigrateTo(BEFORE);
            int id = SavedExecution();
            MigrateTo(AFTER);

            using AppDbContext db = _database.Open();
            db.Executions.Where(x => x.Id == id).Select(x => x.ErrorMessage).Single().ShouldBe("It failed");
            db.ExecutionSteps.Where(x => x.ExecutionId == id).OrderBy(x => x.Sequence).Select(x => x.Name).ToList().ShouldBe(["Find", "Click"]);

            db.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_table_info('Executions') WHERE name LIKE 'Viewport%'")
                .AsEnumerable()
                .Single()
                .ShouldBe(0);
        }

        [Fact]
        public void Steps_still_go_with_their_execution_afterwards()
        {
            MigrateTo(BEFORE);
            int id = SavedExecution();
            MigrateTo(AFTER);

            using AppDbContext db = _database.Open();
            db.Executions.Where(x => x.Id == id).ExecuteDelete();

            db.ExecutionSteps.Count(x => x.ExecutionId == id).ShouldBe(0);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        // As SQL: the table still has the columns, which take no default and the model no longer has.
        private int SavedExecution()
        {
            using AppDbContext db = _database.Open();

            Flow flow = new Flow { Name = "Login", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            db.Database.ExecuteSqlRaw(@"
                INSERT INTO Executions (Status, HistoryLevel, StepCount, CsvRowIndex, ViewportWidth, ViewportHeight, ErrorMessage, FlowStructureHash, FlowId, CreatedOn)
                VALUES ('FAILED', 'STEPS_AND_IMAGES', 2, 0, 0, 0, 'It failed', 'hash', {0}, '2026-11-07');", flow.Id);

            int id = db.Executions.Max(x => x.Id);

            db.Database.ExecuteSqlRaw(@"
                INSERT INTO ExecutionSteps (Sequence, Depth, Name, FlowStepType, Outcome, StartedOn, DurationMilliseconds, ExecutionId, CreatedOn)
                VALUES (0, 0, 'Find', 'SEARCH_IMAGE', 'SUCCESS', '2026-11-07', 10, {0}, '2026-11-07'),
                       (1, 0, 'Click', 'CURSOR_CLICK', 'SUCCESS', '2026-11-07', 10, {0}, '2026-11-07');", id);

            return id;
        }
    }
}
