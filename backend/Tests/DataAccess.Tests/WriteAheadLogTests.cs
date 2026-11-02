using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Tests
{
    /// <summary>The mode lives in the file, so it needs a file: a database in memory has no log to write ahead to.</summary>
    public sealed class WriteAheadLogTests : IDisposable
    {
        private readonly string _file = Path.Combine(Path.GetTempPath(), "sflw-wal-" + Guid.NewGuid().ToString("N")[..8] + ".db");

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            foreach (string path in new[] { _file, _file + "-wal", _file + "-shm" })
                File.Delete(path);
        }

        private void Prepare()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={_file}").Options;

            using AppDbContext db = new AppDbContext(options);
            db.Database.Migrate();
            db.Database.UseWriteAheadLog();
        }

        private string JournalMode()
        {
            using SqliteConnection connection = new SqliteConnection($"Data Source={_file};Pooling=False");
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode;";
            return (string)command.ExecuteScalar()!;
        }

        [Fact]
        public void A_database_is_switched_to_the_write_ahead_log_and_a_later_connection_finds_it_so()
        {
            Prepare();

            JournalMode().ShouldBe("wal");
        }

        // Every start asks again, as the app does.
        [Fact]
        public void Asking_again_on_a_database_already_switched_changes_nothing()
        {
            Prepare();
            Prepare();

            JournalMode().ShouldBe("wal");
        }
    }
}
