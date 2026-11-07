using Core.Models.Database;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    public sealed class RenameUseClientAreaMigrationTests : IDisposable
    {
        private const string BEFORE = "20261107000315_RenameRunCommandToShell";
        private const string AFTER = "20261107030250_RenameUseClientAreaToIgnoreWindowBorders";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public void A_window_keeps_whether_its_borders_are_ignored()
        {
            MigrateTo(BEFORE);
            int inside = SavedArea("Inside the borders", true);
            int whole = SavedArea("The whole window", false);
            MigrateTo(AFTER);

            using AppDbContext db = _database.Open();
            db.FlowAreas.Where(x => x.Id == inside).Select(x => x.IgnoreWindowBorders).Single().ShouldBeTrue();
            db.FlowAreas.Where(x => x.Id == whole).Select(x => x.IgnoreWindowBorders).Single().ShouldBeFalse();
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        private int SavedArea(string name, bool useClientArea)
        {
            using AppDbContext db = _database.Open();

            Flow flow = new Flow { Name = name, PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            return OldRows.AreaWithAddress(db, flow.Id, name, useClientArea);
        }
    }
}
