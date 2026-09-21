using Core.Helpers;
using DataAccess.Interceptors;
using Microsoft.EntityFrameworkCore;
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

            services.AddPooledDbContextFactory<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite($"Data Source={dataSource};");

                // Intercept INSERT/UPDATE commands and fill the CreatedOn/UpdatedOn of the BaseDbModel.
                options.AddInterceptors(serviceProvider.GetRequiredService<TimestampInterceptor>());
            });

            return services;
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