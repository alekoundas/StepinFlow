using Core.Models;
using Core.Models.Database;
using DataAccess.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class AppDbContext : DbContext
    {
        public DbSet<Flow> Flows { get; set; }
        public DbSet<SubFlow> SubFlows { get; set; }
        public DbSet<FlowArea> FlowAreas { get; set; }
        public DbSet<FlowPoint> FlowPoints { get; set; }
        public DbSet<FlowStep> FlowSteps { get; set; }
        public DbSet<FlowStepImage> FlowStepImages { get; set; }
        public DbSet<Execution> Executions { get; set; }
        public DbSet<ExecutionStep> ExecutionSteps { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new FlowConfiguration());
            builder.ApplyConfiguration(new SubFlowConfiguration());
            builder.ApplyConfiguration(new FlowAreaConfiguration());
            builder.ApplyConfiguration(new FlowPointConfiguration());
            builder.ApplyConfiguration(new FlowStepConfiguration());
            builder.ApplyConfiguration(new FlowStepImageConfiguration());
            builder.ApplyConfiguration(new ExecutionConfiguration());
            builder.ApplyConfiguration(new ExecutionStepConfiguration());
            builder.ApplyConfiguration(new AppSettingConfiguration());
        }
    }
}
