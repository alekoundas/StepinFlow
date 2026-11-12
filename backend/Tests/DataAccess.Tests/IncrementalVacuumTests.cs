using Core.Models.Database;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Tests
{
    /// <summary>
    /// What a delete hands back to the disk. It needs a file, since that is where the space is: a
    /// database switched to incremental vacuum, an execution's steps deleted, the file measured.
    /// </summary>
    public sealed class IncrementalVacuumTests : IDisposable
    {
        private readonly string _file = Path.Combine(Path.GetTempPath(), "sflw-vacuum-" + Guid.NewGuid().ToString("N")[..8] + ".db");
        private readonly DbContextOptions<AppDbContext> _options;

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        public IncrementalVacuumTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            foreach (string path in new[] { _file, _file + "-wal", _file + "-shm" })
                File.Delete(path);
        }

        private bool Prepare()
        {
            using AppDbContext db = new AppDbContext(_options);
            db.Database.Migrate();
            db.Database.UseWriteAheadLog();

            return db.Database.UseIncrementalVacuum();
        }

        private long Pragma(string name)
        {
            using SqliteConnection connection = new SqliteConnection($"Data Source={_file};Pooling=False");
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"PRAGMA {name};";
            return (long)command.ExecuteScalar()!;
        }

        // The first start after the switch shipped finds a database that never had it.
        [Fact]
        public void A_database_is_switched_to_incremental_vacuum_once_and_stays_so()
        {
            Prepare().ShouldBeTrue();
            Prepare().ShouldBeTrue();

            Pragma("auto_vacuum").ShouldBe(2);
        }

        [Fact]
        public async Task Deleting_an_executions_steps_hands_their_space_back_to_the_disk()
        {
            Prepare();

            await using (AppDbContext db = new AppDbContext(_options))
            {
                Flow flow = new Flow { Name = "Firestone", PublicId = Guid.NewGuid() };
                db.Flows.Add(flow);
                await db.SaveChangesAsync(Ct);

                Execution execution = new Execution { FlowId = flow.Id };
                db.Executions.Add(execution);
                await db.SaveChangesAsync(Ct);

                db.ExecutionSteps.AddRange(Enumerable.Range(0, 10_000).Select(i => new ExecutionStep
                {
                    ExecutionId = execution.Id,
                    Sequence = i,
                    Name = "Image Search Tavern",
                    Message = new string('x', 200),
                }));
                await db.SaveChangesAsync(Ct);

                await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);", Ct);
            }

            long full = new FileInfo(_file).Length;

            await using (AppDbContext db = new AppDbContext(_options))
            {
                await db.ExecutionSteps.ExecuteDeleteAsync(Ct);
                await db.ReclaimFreeSpaceAsync(Ct);
            }

            Pragma("freelist_count").ShouldBe(0);
            new FileInfo(_file).Length.ShouldBeLessThan(full / 4);
        }
    }
}
