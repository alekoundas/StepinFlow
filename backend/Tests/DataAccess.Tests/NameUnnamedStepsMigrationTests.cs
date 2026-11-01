using Core.Enums;
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

                FlowArea chrome = new FlowArea { Name = "chrome", FlowId = flow.Id, Type = FlowAreaTypeEnum.APPLICATION };
                db.FlowAreas.Add(chrome);
                db.SaveChanges();

                Add(db, flow, new FlowStep { FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = RunCommandPresetEnum.LAUNCH_APP, FlowAreaId = chrome.Id });
                Add(db, flow, new FlowStep { FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = RunCommandPresetEnum.READ_CLIPBOARD });
                Add(db, flow, new FlowStep { FlowStepType = FlowStepTypeEnum.SUB_FLOW, SubFlowId = checkout.Id });
                Add(db, flow, new FlowStep { FlowStepType = FlowStepTypeEnum.NOTIFY });
                Add(db, flow, new FlowStep { FlowStepType = FlowStepTypeEnum.END_EXECUTION });
                Add(db, flow, new FlowStep { FlowStepType = FlowStepTypeEnum.END_EXECUTION });
                Add(db, flow, new FlowStep { FlowStepType = FlowStepTypeEnum.END_EXECUTION, EndExecutionAsSuccess = true });
                Add(db, flow, new FlowStep { FlowStepType = FlowStepTypeEnum.END_EXECUTION, Name = "Give up" });
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

        private static void Add(AppDbContext db, Flow flow, FlowStep step)
        {
            step.RootId = flow.Id;
            step.FlowId = flow.Id;
            step.OrderNumber = db.FlowSteps.Count();

            db.FlowSteps.Add(step);
            db.SaveChanges();
        }

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }
    }
}
