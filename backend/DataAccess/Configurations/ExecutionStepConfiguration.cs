using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class ExecutionStepConfiguration : IEntityTypeConfiguration<ExecutionStep>
    {
        public void Configure(EntityTypeBuilder<ExecutionStep> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Outcome).HasConversion<string>();
            builder.Property(x => x.FlowStepType).HasConversion<string>();

            builder.HasOne(x => x.Execution)
                .WithMany(x => x.ExecutionSteps)
                .HasForeignKey(x => x.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deleting one step out of a flow must not erase every run that touched it, so the
            // link is cleared and the copied name and type keep the row meaningful.
            builder.HasOne(x => x.FlowStep)
                .WithMany()
                .HasForeignKey(x => x.FlowStepId)
                .OnDelete(DeleteBehavior.SetNull);

            // The two location columns are one value in code and never queried apart.
            builder.Ignore(x => x.Location);

            // How a run is always read: its own steps, in the order they happened.
            builder.HasIndex(x => new { x.ExecutionId, x.Sequence });
        }
    }
}
