using Core.Models.Database;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    /// <summary>
    /// A launch of an area carried its whole command line. Its program becomes what starts the
    /// area, and the rest the launch's arguments.
    /// </summary>
    public sealed class LaunchArgumentsMigrationTests : IDisposable
    {
        private const string BEFORE = "20261031201705_NameBranchRows";
        private const string AFTER = "20261101005153_LaunchArguments";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public void A_launchs_program_starts_its_area_and_the_rest_are_its_arguments()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int areaId = SavedArea(flowId, "App");
            int quoted = SavedLaunch(flowId, areaId, "\"C:\\Program Files\\App\\app.exe\" --profile test");
            int bare = SavedLaunch(flowId, areaId, "\"C:\\Program Files\\App\\app.exe\"");
            MigrateTo(AFTER);

            using AppDbContext db = _database.Open();
            db.FlowAreas.Single(x => x.Id == areaId).LaunchCommand.ShouldBe("\"C:\\Program Files\\App\\app.exe\"");
            Launch(db, quoted).ShouldBe(("", "--profile test", areaId));
            Launch(db, bare).ShouldBe(("", "", areaId));
        }

        // Starting the area's program instead would start something else with nobody told.
        [Fact]
        public void A_launch_of_another_program_keeps_it_and_lets_go_of_the_area()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int areaId = SavedArea(flowId, "Browser");
            SavedLaunch(flowId, areaId, "chrome.exe https://example.com");
            int other = SavedLaunch(flowId, areaId, "msedge.exe https://example.com");
            MigrateTo(AFTER);

            using AppDbContext db = _database.Open();
            db.FlowAreas.Single(x => x.Id == areaId).LaunchCommand.ShouldBe("chrome.exe");
            Launch(db, other).ShouldBe(("msedge.exe https://example.com", "", (int?)null));
        }

        [Fact]
        public void A_launch_with_no_area_keeps_its_command_line()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int launch = SavedLaunch(flowId, null, "notepad.exe notes.txt");
            MigrateTo(AFTER);

            using AppDbContext db = _database.Open();
            Launch(db, launch).ShouldBe(("notepad.exe notes.txt", "", (int?)null));
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        private static (string Command, string Arguments, int? AreaId) Launch(AppDbContext db, int id)
        {
            FlowStep step = db.FlowSteps.Single(x => x.Id == id);
            return (step.RunCommandValue, step.LaunchArguments, step.FlowAreaId);
        }

        private int SavedFlow()
        {
            using AppDbContext db = _database.Open();

            Flow flow = new Flow { Name = "Login", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            return flow.Id;
        }

        private int SavedArea(int flowId, string name)
        {
            using AppDbContext db = _database.Open();
            return OldRows.Area(db, flowId, name);
        }

        private int SavedLaunch(int flowId, int? areaId, string command)
        {
            using AppDbContext db = _database.Open();
            return OldRows.Step(db, flowId, null, "SYSTEM_COMMAND", "", 0, areaId, "LAUNCH_APP", command);
        }
    }
}
