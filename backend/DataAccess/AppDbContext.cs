using Core.Models;
using Core.Models.Database;
using DataAccess.Configurations;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class AppDbContext : DbContext
    {
        public DbSet<Flow> Flows { get; set; }
        public DbSet<FlowArea> FlowAreas { get; set; }
        public DbSet<FlowPoint> FlowPoints { get; set; }
        public DbSet<FlowViewport> FlowViewports { get; set; }
        public DbSet<FlowCsvColumn> FlowCsvColumns { get; set; }
        public DbSet<FlowStep> FlowSteps { get; set; }
        public DbSet<FlowStepTemplate> FlowStepTemplates { get; set; }
        public DbSet<FlowStepLastGoodScreenshotHistory> FlowStepLastGoodScreenshotHistories { get; set; }
        public DbSet<Execution> Executions { get; set; }
        public DbSet<ExecutionStep> ExecutionSteps { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<DiscordBot> DiscordBots { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new FlowConfiguration());
            modelBuilder.ApplyConfiguration(new FlowAreaConfiguration());
            modelBuilder.ApplyConfiguration(new FlowPointConfiguration());
            modelBuilder.ApplyConfiguration(new FlowStepConfiguration());
            modelBuilder.ApplyConfiguration(new FlowStepTemplateConfiguration());
            modelBuilder.ApplyConfiguration(new FlowStepLastGoodScreenshotHistoryConfiguration());
            modelBuilder.ApplyConfiguration(new ExecutionConfiguration());
            modelBuilder.ApplyConfiguration(new ExecutionStepConfiguration());
            modelBuilder.ApplyConfiguration(new AppSettingConfiguration());
        }
    }
}
