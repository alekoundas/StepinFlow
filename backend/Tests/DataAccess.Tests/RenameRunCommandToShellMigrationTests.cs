using Core.Enums;
using Core.Models.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    /// <summary>
    /// The System Command columns took Shell names, each keeping what it held. EF scaffolded these
    /// renames with the columns paired up wrongly, so every column has its own value here.
    /// </summary>
    public sealed class RenameRunCommandToShellMigrationTests : IDisposable
    {
        private const string BEFORE = "20261104002217_StepIsDisabled";
        private const string AFTER = "20261107000315_RenameRunCommandToShell";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public void Every_column_keeps_what_it_held()
        {
            MigrateTo(BEFORE);
            int id = SavedCommand();
            MigrateTo(AFTER);

            using AppDbContext db = _database.Open();
            FlowStep step = db.FlowSteps
                .Where(x => x.Id == id)
                .Select(x => new FlowStep
                {
                    ShellType = x.ShellType,
                    ShellCommandMode = x.ShellCommandMode,
                    ShellCommandValue = x.ShellCommandValue,
                    ShellWorkingDirectory = x.ShellWorkingDirectory,
                    ShellArguments = x.ShellArguments,
                    ShellSuccessExitCodes = x.ShellSuccessExitCodes,
                    ShellResultSource = x.ShellResultSource,
                })
                .Single();

            step.ShouldSatisfyAllConditions(
                x => x.ShellType.ShouldBe(ShellTypeEnum.POWERSHELL),
                x => x.ShellCommandMode.ShouldBe(ShellCommandModeEnum.KILL_PROCESS),
                x => x.ShellCommandValue.ShouldBe("chrome.exe"),
                x => x.ShellWorkingDirectory.ShouldBe(@"C:\app"),
                x => x.ShellArguments.ShouldBe("--quiet"),
                x => x.ShellSuccessExitCodes.ShouldBe("0,3"),
                x => x.ShellResultSource.ShouldBe(ShellResultSourceEnum.STDERR));
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        private int SavedCommand()
        {
            using AppDbContext db = _database.Open();

            Flow flow = new Flow { Name = "Login", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            int id = OldRows.Step(db, flow.Id, null, "SYSTEM_COMMAND", "Close it", 0, runCommandPreset: "KILL_PROCESS", runCommandValue: "chrome.exe");

            db.Database.ExecuteSqlRaw(@"
                UPDATE FlowSteps
                SET RunCommandShell = 'POWERSHELL', RunCommandWorkingDirectory = 'C:\app', LaunchArguments = '--quiet',
                    SuccessExitCodes = '0,3', ResultSource = 'STDERR'
                WHERE Id = {0};", id);

            return id;
        }
    }
}
