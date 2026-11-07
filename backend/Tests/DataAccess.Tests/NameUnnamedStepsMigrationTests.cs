using Core.Models.Database;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    /// <summary>
    /// An import or a recording saved End, Notify, Launch, command and sub-flow steps with no name.
    /// Each gets what an import names it now, and a step with a name keeps it.
    /// </summary>
    public sealed class NameUnnamedStepsMigrationTests : IDisposable
    {
        private const string BEFORE = "20261101131919_DropTemplateRequired";
        private const string AFTER = "20261101142125_NameUnnamedSteps";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public void Each_is_named_for_what_it_is_numbered_within_its_flow()
        {
            MigrateTo(BEFORE);

            int flowId;
            using (AppDbContext db = _database.Open())
            {
                Flow flow = new Flow { Name = "Login", PublicId = Guid.NewGuid() };
                Flow checkout = new Flow { Name = "Checkout", PublicId = Guid.NewGuid(), IsSubFlow = true };
                db.Flows.AddRange(flow, checkout);
                db.SaveChanges();
                flowId = flow.Id;

                int chromeId = OldRows.AreaWithAddress(db, flow.Id, "chrome");

                OldRows.Step(db, flow.Id, null, "SYSTEM_COMMAND", "", 0, chromeId, "LAUNCH_APP");
                OldRows.Step(db, flow.Id, null, "SYSTEM_COMMAND", "", 1, runCommandPreset: "READ_CLIPBOARD");
                OldRows.Step(db, flow.Id, null, "SUB_FLOW", "", 2, subFlowId: checkout.Id);
                OldRows.Step(db, flow.Id, null, "NOTIFY", "", 3);
                OldRows.Step(db, flow.Id, null, "END_EXECUTION", "", 4);
                OldRows.Step(db, flow.Id, null, "END_EXECUTION", "", 5);
                OldRows.Step(db, flow.Id, null, "END_EXECUTION", "", 6, endExecutionAsSuccess: true);
                OldRows.Step(db, flow.Id, null, "END_EXECUTION", "Give up", 7);
            }

            MigrateTo(AFTER);

            using AppDbContext after = _database.Open();
            after.FlowSteps
                .Where(x => x.RootId == flowId)
                .OrderBy(x => x.OrderNumber)
                .Select(x => x.Name)
                .ToList()
                .ShouldBe(["Launch chrome", "Read clipboard", "Checkout", "Notify", "End as failed", "End as failed 2", "End as passed", "Give up"]);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }
    }
}
