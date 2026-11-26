using Core.Enums;
using Core.Models.Database;
using DataAccess;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace App.Tests.Hosting
{
    /// <summary>
    /// What the backend does to its database before anything else starts. A file, since the write
    /// ahead log lives in one.
    /// </summary>
    public sealed class HostedStartupServiceTests : IDisposable
    {
        private const long INCREMENTAL = 2;

        private readonly string _file = Path.Combine(Path.GetTempPath(), "sflw-startup-" + Guid.NewGuid().ToString("N")[..8] + ".db");
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly HostedStartupService _startup;

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        public HostedStartupServiceTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={_file}").Options;
            _startup = new HostedStartupService(new FileContextFactory(_options), TimeProvider.System, NullLogger<HostedStartupService>.Instance);
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            foreach (string path in new[] { _file, _file + "-wal", _file + "-shm" })
                File.Delete(path);
        }

        [Fact]
        public async Task A_new_database_is_migrated_and_set_up()
        {
            await _startup.StartingAsync(Ct);

            using (AppDbContext db = new AppDbContext(_options))
                db.Database.GetPendingMigrations().ShouldBeEmpty();

            Pragma("journal_mode").ShouldBe("wal");
            Pragma("auto_vacuum").ShouldBe(INCREMENTAL);
        }

        // As on the launch after the app went away with it going.
        [Fact]
        public async Task An_execution_left_running_is_abandoned()
        {
            await _startup.StartingAsync(Ct);
            int id = SavedRunningExecution();

            await _startup.StartingAsync(Ct);

            using AppDbContext db = new AppDbContext(_options);
            db.Executions.Single(x => x.Id == id).Status.ShouldBe(ExecutionStatusEnum.ABANDONED);
        }

        private int SavedRunningExecution()
        {
            using AppDbContext db = new AppDbContext(_options);

            Flow flow = new Flow { Name = "Looping", PublicId = Guid.NewGuid() };
            db.Flows.Add(flow);
            db.SaveChanges();

            Execution execution = new Execution { FlowId = flow.Id, Status = ExecutionStatusEnum.RUNNING };
            db.Executions.Add(execution);
            db.SaveChanges();

            return execution.Id;
        }

        private object Pragma(string name)
        {
            using SqliteConnection connection = new SqliteConnection($"Data Source={_file};Pooling=False");
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"PRAGMA {name};";
            return command.ExecuteScalar()!;
        }

        private sealed class FileContextFactory : IDbContextFactory<AppDbContext>
        {
            private readonly DbContextOptions<AppDbContext> _options;

            public FileContextFactory(DbContextOptions<AppDbContext> options)
            {
                _options = options;
            }

            public AppDbContext CreateDbContext()
            {
                return new AppDbContext(_options);
            }
        }
    }
}
