using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowStepLastGoodScreenshotHistoryConfiguration : IEntityTypeConfiguration<FlowStepLastGoodScreenshotHistory>
    {
        public void Configure(EntityTypeBuilder<FlowStepLastGoodScreenshotHistory> builder)
        {
            builder.HasKey(x => x.Id);


            // Relationship with FlowStep (one-to-many)
            builder.HasOne(x => x.FlowStep)
                .WithMany(x => x.FlowStepLastGoodScreenshotHistories)
                .HasForeignKey(x => x.FlowStepId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            // One row per step and viewport: a passing execution replaces the last.
            builder.HasIndex(x => new { x.FlowStepId, x.ViewportWidth, x.ViewportHeight }).IsUnique();
        }
    }
}
