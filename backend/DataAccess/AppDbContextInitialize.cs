using Core.Enums;
using Core.Helpers;
using DataAccess.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Globalization;

namespace DataAccess
{

    /// <summary>
    /// Sets the database file settings and handles first launch.
    /// Also manages vaccuming of the db file to srink in size.
    /// 
    /// Page size    = 4kb.
    /// Vacuum mode  = INCREMENTAL.
    /// Journal mode = WAL.
    /// </summary>
    public static class AppDbContextInitialize
    {

        // Auto-Vacuum Modes!
        // 0 = NONE(Default)   Free pages stay inside the file for reuse.
        // 1 = Full            Automatically moves free pages to the end of the file and truncates the file on every transaction commit.
        // 2 = INCREMENTAL     Tracks free pages so they can be removed. Run PRAGMA incremental_vacuum to shrink the file.
        private const long VACUUM_MODE = 2;
        private const int CHECKPOINT_ATTEMPTS = 300; // How long EmptyWALAsync keeps trying: 300 times, 100 ms apart, about 30s.

        private static readonly TimeSpan CHECKPOINT_RETRY = TimeSpan.FromMilliseconds(100);


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary> Create the Database file on the disk if needed and set DbContextFactory along with the interceptors. </summary>
        public static void AddCustomDbContextFactory(IServiceCollection services)
        {

            string dataSource = PathHelper.GetDatabaseFilePath();

            // Create the database file if it doesn’t exist.
            if (!File.Exists(dataSource))
                File.Create(dataSource).Close();

            services.AddSingleton<TimestampInterceptor>();
            services.AddSingleton<TemplateThumbnailInterceptor>();

            services.AddPooledDbContextFactory<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite($"Data Source={dataSource};");

                // Intercept INSERT/UPDATE commands and fill the CreatedOn/UpdatedOn of the BaseDbModel.
                options.AddInterceptors(serviceProvider.GetRequiredService<TimestampInterceptor>());

                // A template thumbnail is made whenever its picture is saved.
                options.AddInterceptors(serviceProvider.GetRequiredService<TemplateThumbnailInterceptor>());
            });
        }

        /// <summary>
        /// Switches the SQLite database to Write-Ahead Logging mode (WAL), which makes concurrent reads and writes significantly faster.
        /// Instead of modifying the main database file directly and locking out readers, SQLite appends changes to a separate -wal file 
        /// and later merges them back into the main database during a "checkpoint."
        /// </summary>
        public static void SetWALMode(AppDbContext dbContext)
        {
            dbContext.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        }

        /// <summary> Sets vacuum mode to INCREMENTAL for the first launch. </summary>
        public static bool SetVacuumMode(AppDbContext dbContext)
        {
            // Check if mode is alrady set.
            if (ReadPragma(dbContext, "auto_vacuum") == VACUUM_MODE)
                return true;

            // Set the vacuum mode.
            dbContext.Database.OpenConnection();

            try
            {
                dbContext.Database.ExecuteSqlRaw("PRAGMA auto_vacuum = INCREMENTAL;");
                dbContext.Database.ExecuteSqlRaw("VACUUM;");
                dbContext.Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(TRUNCATE);"); // The rewrite went through the log, which is now the size of the file.
            }
            catch (SqliteException)
            {
                return false;
            }
            finally
            {
                dbContext.Database.CloseConnection();
            }

            bool isSuccess = ReadPragma(dbContext, "auto_vacuum") == VACUUM_MODE;
            return isSuccess;
        }

        /// <summary> Moves free pages to the end of the Database file truncates them. </summary>
        public static async Task ReclaimFreePagesAsync(AppDbContext dbContext, int pages, CancellationToken ct = default)
        {
            DbConnection connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync(ct);

            try
            {
                // It frees a page a row, and only while it is read.
                await using DbCommand vacuum = connection.CreateCommand();
                vacuum.CommandText = $"PRAGMA incremental_vacuum({pages.ToString(CultureInfo.InvariantCulture)});";

                await using DbDataReader reader = await vacuum.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                }
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        /// <summary>
        /// Empties the -wal file into it the Database file. 
        /// Uses checkpoint with TRUNCATE mode and locks writes.
        /// Keeps retrying untill no writes going on and every reader done with the -wal.
        /// </summary>
        public static async Task<bool> EmptyWALAsync(AppDbContext dbContext, CancellationToken ct = default)
        {
            int attempts = 1;
            while (!await TryEmptyWALAsync(dbContext, ct))
            {
                if (attempts == CHECKPOINT_ATTEMPTS)
                    return false;

                attempts++;
                await Task.Delay(CHECKPOINT_RETRY, ct);
            }

            return true;
        }

        /// <summary>The bytes of a page in the database file.</summary>
        public static Task<long> PageSizeAsync(AppDbContext dbContext, CancellationToken ct = default)
        {
            return ReadPragmaAsync(dbContext, "page_size", ct);
        }

        /// <summary>Count of the pages in the database file.</summary>
        public static Task<long> PageCountAsync(AppDbContext dbContext, CancellationToken ct = default)
        {
            return ReadPragmaAsync(dbContext, "page_count", ct);
        }

        /// <summary>Count the empty pages.</summary>
        public static Task<long> FreePageCountAsync(AppDbContext dbContext, CancellationToken ct = default)
        {
            return ReadPragmaAsync(dbContext, "freelist_count", ct);
        }

        /// <summary>Checks vacum mode (SetVacuumMode could not switch it on).</summary>
        public static async Task<bool> IsIncrementalVacuumEnabledAsync(AppDbContext dbContext, CancellationToken ct = default)
        {
            return await ReadPragmaAsync(dbContext, "auto_vacuum", ct) == VACUUM_MODE;
        }

        /// <summary> An execution still in RUNNING mode as the app starts is marked as ABANDONED. </summary>
        public static async Task AbandonUnfinishedExecutionsAsync(AppDbContext dbContext, TimeProvider timeProvider)
        {
            await dbContext.Executions
                .Where(x => x.Status == ExecutionStatusEnum.RUNNING)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(execution => execution.Status, ExecutionStatusEnum.ABANDONED)
                    .SetProperty(execution => execution.CompletedAt, timeProvider.GetUtcNow().UtcDateTime)
                    .SetProperty(execution => execution.StepCount, execution => execution.ExecutionSteps.Count()));
        }


        // ================================================================
        // Private methods
        // ================================================================
        private static async Task<long> ReadPragmaAsync(AppDbContext dbContext, string name, CancellationToken ct)
        {
            DbConnection connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync(ct);

            try
            {
                await using DbCommand command = connection.CreateCommand();
                command.CommandText = $"PRAGMA {name};";

                return Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        private static long ReadPragma(AppDbContext dbContext, string name)
        {
            DbConnection connection = dbContext.Database.GetDbConnection();
            dbContext.Database.OpenConnection();

            try
            {
                using DbCommand command = connection.CreateCommand();
                command.CommandText = $"PRAGMA {name};";

                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
            finally
            {
                dbContext.Database.CloseConnection();
            }
        }

        // A checkpoint that cannot finish is not an error: busy = 1.
        private static async Task<bool> TryEmptyWALAsync(AppDbContext dbContext, CancellationToken ct)
        {
            DbConnection connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync(ct);

            try
            {
                await using DbCommand checkpoint = connection.CreateCommand();
                checkpoint.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";

                await using DbDataReader row = await checkpoint.ExecuteReaderAsync(ct);
                return await row.ReadAsync(ct) && row.GetInt64(0) == 0;
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }
}