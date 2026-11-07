using Core.Enums;
using Core.Helpers;
using DataAccess.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess
{
    public static class AppDbContextInitialize
    {
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