using Core.Models.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    /// <summary>
    /// A browser tab is found by its browser and its address: the address it was matched on is
    /// kept, and the window title that narrowed its browser goes.
    /// </summary>
    public sealed class BrowserTabAddressMigrationTests : IDisposable
    {
        private const string BEFORE = "20261101005153_LaunchArguments";
        private const string AFTER = "20261101011734_BrowserTabAddress";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public void A_tab_keeps_its_address_and_loses_its_title()
        {
            MigrateTo(BEFORE);
            int flowId = SavedFlow();
            int tab = SavedArea(flowId, "Crude oil", "BROWSER_TAB", "Chrome", "tradingeconomics.com/commodity/crude-oil");
            int window = SavedArea(flowId, "Game", "APPLICATION", "Firestone", "");
            MigrateTo(AFTER);

            using AppDbContext db = _database.Open();
            FlowArea tabArea = db.FlowAreas
                .Where(x => x.Id == tab)
                .Select(x => new FlowArea { TabAddress = x.TabAddress, TitlePattern = x.TitlePattern })
                .Single();
            (tabArea.TabAddress, tabArea.TitlePattern).ShouldBe(("tradingeconomics.com/commodity/crude-oil", ""));
            db.FlowAreas.Where(x => x.Id == window).Select(x => x.TitlePattern).Single().ShouldBe("Firestone");
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

            Flow flow = new Flow { Name = "Prices", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            return flow.Id;
        }

        // As SQL: the model has a column the table does not have yet, and not one it still has.
        private int SavedArea(int flowId, string name, string type, string titlePattern, string tabMatchValue)
        {
            using AppDbContext db = _database.Open();

            db.Database.ExecuteSqlRaw(@"
                INSERT INTO FlowAreas (Name, Type, SizingMode, LocationX, LocationY, Width, Height, RatioX, RatioY, RatioWidth, RatioHeight,
                    ProcessName, TitlePattern, TitleMatchMode, UseClientArea, TabMatchValue, TabMatchOn, MonitorDeviceName, FlowId,
                    CreatedOn, UpdatedOn, AuthoredDpi, IsMain, LaunchCommand)
                VALUES ({0}, {1}, 'ABSOLUTE_PX', 0, 0, 0, 0, 0, 0, 0, 0,
                    'chrome', {2}, 'CONTAINS', 1, {3}, 'URL', '', {4},
                    '2026-11-01', '2026-11-01', 0, 0, '');", name, type, titlePattern, tabMatchValue, flowId);

            return db.FlowAreas.Max(x => x.Id);
        }
    }
}
