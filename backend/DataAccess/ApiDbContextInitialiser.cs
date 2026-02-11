using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataAccess
{
    public class ApiDbContextInitialiser
    {
        private readonly ILogger<ApiDbContextInitialiser> _logger;
        private readonly IDbContextFactory<ApiDbContext> _dbContextFactory;
        private readonly IServiceProvider _serviceProvider;

        public ApiDbContextInitialiser(
            ILogger<ApiDbContextInitialiser> logger,
            IDbContextFactory<ApiDbContext> dbContextFactory,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
            _serviceProvider = serviceProvider;
        }

        public void RunMigrations()
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                dbContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running migrations.");
                throw;
            }
        }

        public void Seed()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                //var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

                //await TrySeedAdminRolesAndClaimsAsync(roleManager);
                //await TrySeedTrainerRolesAndClaimsAsync(roleManager);
                //await TrySeedSimpleUserRolesAndClaimsAsync(roleManager);

                //await TrySeedAdminUserAsync(userManager, roleManager);
                //await TrySeedTrainerUserAsync(userManager, roleManager);
                //await TrySeedSimpleUserUserAsync(userManager, roleManager);

                return;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
    }
}