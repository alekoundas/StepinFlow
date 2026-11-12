using System.Data.Common;
using System.Globalization;

using Core.Enums;
using Core.Helpers;
using DataAccess.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess
{
    public static class AppDbContextInitialize
    {
        private const long AUTO_VACUUM_INCREMENTAL = 2;

        public static IServiceCollection AddCustomDbContextFactory(this IServiceCollection services)
        {

            string dataPath = PathHelper.GetDatabaseDataPath();
            string dataSource = Path.Combine(dataPath, "StepinFlowSQLite.db");

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

                // A template's thumbnail is made whenever its picture is saved.
                options.AddInterceptors(serviceProvider.GetRequiredService<TemplateThumbnailInterceptor>());
            });

            return services;
        }

        /// <summary>
        /// A save goes on the end of a log beside the file rather than through a journal made, synced
        /// and deleted every time, and reading never waits for a save. The file keeps the mode, so
        /// only the first call changes anything. The database is then three files - the .db, its
        /// -wal and its -shm - and a copy taken while the app is open needs all three.
        /// </summary>
        public static void UseWriteAheadLog(this DatabaseFacade database)
        {
            database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        }

        /// <summary>
        /// Lets a delete hand its space back to the disk, through ReclaimFreeSpaceAsync, rather than
        /// leave the file at the largest it ever was. Switching it on rewrites the file once - a few
        /// seconds a gigabyte - and the file keeps the mode, so later calls change nothing. False when
        /// the switch failed, out of disk space say, which costs the space and nothing else.
        /// </summary>
        public static bool UseIncrementalVacuum(this DatabaseFacade database)
        {
            if (ReadPragma(database, "auto_vacuum") == AUTO_VACUUM_INCREMENTAL)
                return true;

            // One connection throughout: the mode asked for belongs to the connection that asked,
            // until a VACUUM on that same connection writes it into the file.
            database.OpenConnection();

            try
            {
                database.ExecuteSqlRaw("PRAGMA auto_vacuum = INCREMENTAL;");
                database.ExecuteSqlRaw("VACUUM;");
                database.ExecuteSqlRaw("PRAGMA wal_checkpoint(TRUNCATE);"); // The rewrite went through the log, which is now the size of the file.
            }
            catch (SqliteException)
            {
                return false;
            }
            finally
            {
                database.CloseConnection();
            }

            return ReadPragma(database, "auto_vacuum") == AUTO_VACUUM_INCREMENTAL;
        }

        /// <summary>
        /// Hands the pages deletes freed back to the disk, then empties the log beside the file. Only
        /// what sits past a hole is moved, so it costs what was deleted, not what is kept. Never
        /// inside a transaction.
        /// </summary>
        public static async Task ReclaimFreeSpaceAsync(this AppDbContext dbContext, CancellationToken ct = default)
        {
            DbConnection connection = dbContext.Database.GetDbConnection();
            await dbContext.Database.OpenConnectionAsync(ct);

            try
            {
                // It frees a page a row, and only while it is read.
                await using (DbCommand vacuum = connection.CreateCommand())
                {
                    vacuum.CommandText = "PRAGMA incremental_vacuum;";

                    await using DbDataReader reader = await vacuum.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                    }
                }

                await using DbCommand checkpoint = connection.CreateCommand();
                checkpoint.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                await checkpoint.ExecuteNonQueryAsync(ct);
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        /// <summary>
        /// An execution still RUNNING as the app starts was going when the app went away. It is marked
        /// abandoned, and its steps are counted from what its history kept, since its walk never got
        /// to write how many it executed.
        /// </summary>
        public static async Task AbandonUnfinishedExecutionsAsync(this AppDbContext dbContext, TimeProvider timeProvider)
        {
            await dbContext.Executions
                .Where(x => x.Status == ExecutionStatusEnum.RUNNING)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(execution => execution.Status, ExecutionStatusEnum.ABANDONED)
                    .SetProperty(execution => execution.CompletedAt, timeProvider.GetUtcNow().UtcDateTime)
                    .SetProperty(execution => execution.StepCount, execution => execution.ExecutionSteps.Count()));
        }

        private static long ReadPragma(DatabaseFacade database, string name)
        {
            DbConnection connection = database.GetDbConnection();
            database.OpenConnection();

            try
            {
                using DbCommand command = connection.CreateCommand();
                command.CommandText = $"PRAGMA {name};";

                return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
            finally
            {
                database.CloseConnection();
            }
        }




        //public void Seed()
        //{
        //    try
        //    {
        //        using var scope = _serviceProvider.CreateScope();
        //        //var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

        //        //await TrySeedAdminRolesAndClaimsAsync(roleManager);
        //        //await TrySeedTrainerRolesAndClaimsAsync(roleManager);
        //        //await TrySeedSimpleUserRolesAndClaimsAsync(roleManager);

        //        //await TrySeedAdminUserAsync(userManager, roleManager);
        //        //await TrySeedTrainerUserAsync(userManager, roleManager);
        //        //await TrySeedSimpleUserUserAsync(userManager, roleManager);

        //        return;

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "An error occurred while seeding the database.");
        //        throw;
        //    }
        //}
    }
}