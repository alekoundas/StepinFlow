using Core.Enums;
using Core.Models.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    /// <summary>
    /// An area a launch opens is started by that launch's command line, and the launch keeps it.
    /// </summary>
    public sealed class AreaLaunchCommandMigrationTests : IDisposable
    {
        private const string BEFORE = "20261031201705_NameBranchRows";
        private const string AFTER = "20261101000702_AreaLaunchCommand";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public void An_area_a_launch_opens_is_started_by_the_first_launch()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int areaId = SavedArea(flowId, "App");
            SavedLaunch(flowId, areaId, "");
            SavedLaunch(flowId, areaId, "\"C:\\Program Files\\App\\app.exe\" --first");
            SavedLaunch(flowId, areaId, "app.exe --second");
            MigrateTo(AFTER);

            using AppDbContext db = _database.Open();
            db.FlowAreas.Single(x => x.Id == areaId).LaunchCommand.ShouldBe("\"C:\\Program Files\\App\\app.exe\" --first");
            db.FlowSteps.Where(x => x.FlowAreaId == areaId).OrderBy(x => x.Id).Select(x => x.RunCommandValue).ToList()
                .ShouldBe(["", "\"C:\\Program Files\\App\\app.exe\" --first", "app.exe --second"]);
        }

        [Fact]
        public void An_area_no_launch_opens_is_started_by_nothing()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int areaId = SavedArea(flowId, "App");
            MigrateTo(AFTER);

            using AppDbContext db = _database.Open();
            db.FlowAreas.Single(x => x.Id == areaId).LaunchCommand.ShouldBe("");
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        private int SavedFlow()
        {
            using AppDbContext db = _database.Open();

            Flow flow = new Flow { Name = "Login", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            return flow.Id;
        }

        // Written as SQL: the model has the column the table does not have yet.
        private int SavedArea(int flowId, string name)
        {
            using AppDbContext db = _database.Open();

            db.Database.ExecuteSqlRaw(@"
                INSERT INTO FlowAreas (Name, Type, SizingMode, LocationX, LocationY, Width, Height, RatioX, RatioY, RatioWidth, RatioHeight,
                    ProcessName, TitlePattern, TitleMatchMode, UseClientArea, TabMatchValue, TabMatchOn, MonitorDeviceName, FlowId,
                    CreatedOn, UpdatedOn, AuthoredDpi, IsMain)
                VALUES ({0}, 'APPLICATION', 'ABSOLUTE_PX', 0, 0, 0, 0, 0, 0, 0, 0,
                    'app', '', 'CONTAINS', 1, '', 'URL', '', {1},
                    '2026-11-01', '2026-11-01', 0, 0);", name, flowId);

            return db.Database.SqlQuery<int>($"SELECT Id AS Value FROM FlowAreas WHERE Name = {name}").Single();
        }

        private void SavedLaunch(int flowId, int areaId, string command)
        {
            using AppDbContext db = _database.Open();

            db.FlowSteps.Add(new FlowStep
            {
                FlowId = flowId,
                RootId = flowId,
                FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND,
                RunCommandPreset = RunCommandPresetEnum.LAUNCH_APP,
                RunCommandValue = command,
                FlowAreaId = areaId,
            });
            db.SaveChanges();
        }
    }
}
