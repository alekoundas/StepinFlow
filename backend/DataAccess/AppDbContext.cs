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
        public DbSet<FlowSearchArea> FlowSearchAreas { get; set; }
        public DbSet<FlowStep> FlowSteps { get; set; }
        public DbSet<FlowStepImage> FlowStepImages { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
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
