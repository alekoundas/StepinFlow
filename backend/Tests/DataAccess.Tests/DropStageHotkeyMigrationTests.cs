using Core.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccess.Tests
{
    public sealed class DropStageHotkeyMigrationTests : IDisposable
    {
        private const string BEFORE = "20261112175218_ShapeAndColour";
        private const string AFTER = "20261112215935_DropStageHotkey";

        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        // Each key a distinct value, so one moved onto another would show.
        [Fact]
        public void The_stage_key_goes_and_every_other_setting_still_loads()
        {
            MigrateTo(BEFORE);

            using (AppDbContext db = _database.Open())
            {
                db.Database.ExecuteSql($"INSERT INTO AppSettings (\"Key\", Value) VALUES ('RECORDING_HOTKEY_PAUSE', 'VcF6')");
                db.Database.ExecuteSql($"INSERT INTO AppSettings (\"Key\", Value) VALUES ('RECORDING_HOTKEY_STAGE', 'VcF7')");
                db.Database.ExecuteSql($"INSERT INTO AppSettings (\"Key\", Value) VALUES ('RECORDING_HOTKEY_STOP', 'VcF12')");
                db.Database.ExecuteSql($"INSERT INTO AppSettings (\"Key\", Value) VALUES ('HOTKEY_STOP', 'VcF8')");
            }

            MigrateTo(AFTER);

            Read().ShouldBe(new Dictionary<AppSettingKeyEnum, string>
            {
                [AppSettingKeyEnum.RECORDING_HOTKEY_PAUSE] = "VcF6",
                [AppSettingKeyEnum.RECORDING_HOTKEY_STOP] = "VcF12",
                [AppSettingKeyEnum.HOTKEY_STOP] = "VcF8",
            }, ignoreOrder: true);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void MigrateTo(string migration)
        {
            using AppDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        // As the Settings page reads them, so every row left has to load as a setting.
        private Dictionary<AppSettingKeyEnum, string> Read()
        {
            using AppDbContext db = _database.Open();
            return db.AppSettings.AsNoTracking().ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
