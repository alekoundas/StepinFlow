using Core.Helpers;
using Core.Models;
using Core.Models.Database;
using DataAccess.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccess
{
    public class ApiDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public ApiDbContext(DbContextOptions<ApiDbContext> options, IConfiguration configuration)
        : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<Flow> Flows { get; set; }
        public DbSet<SubFlow> SubFlows { get; set; }
        public DbSet<FlowSearchArea> FlowSearchAreas { get; set; }
        public DbSet<FlowStep> FlowSteps { get; set; }
        public DbSet<FlowStepImage> FlowStepImages { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            string dataPath = PathHelper.GetDatabaseDataPath();
            string dataSource = Path.Combine(dataPath, "StepinFlowSQLite.db");

            // Create the database file if it doesn’t exist.
            if (!File.Exists(dataSource))
                File.Create(dataSource).Close();

            optionsBuilder.UseSqlite($"Data Source={dataSource};");


            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new FlowConfiguration());
            builder.ApplyConfiguration(new SubFlowConfiguration());
            builder.ApplyConfiguration(new FlowSearchAreaConfiguration());
            builder.ApplyConfiguration(new FlowStepConfiguration());
            builder.ApplyConfiguration(new FlowStepImageConfiguration());
        }
    }
}
