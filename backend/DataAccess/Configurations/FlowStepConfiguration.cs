using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowStepConfiguration : IEntityTypeConfiguration<FlowStep>
    {
        public void Configure(EntityTypeBuilder<FlowStep> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);


            // Properties - Store enum as string

            builder.Property(x => x.FlowStepType).HasConversion<string>();
            builder.Property(x => x.ConditionType).HasConversion<string>();
            builder.Property(x => x.CursorActionType).HasConversion<string>();
            builder.Property(x => x.CursorButtonType).HasConversion<string>();
            builder.Property(x => x.CursorScrollDirectionType).HasConversion<string>();
            builder.Property(x => x.KeyboardInputType).HasConversion<string>();


            // Relationship with Flow (one-to-many)
            builder.HasOne(x => x.Flow)
                .WithMany(x => x.FlowSteps  )
                .HasForeignKey(x => x.FlowId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            // Relationship with SubFlow (one-to-many)
            builder.HasOne(x => x.SubFlow)
                .WithMany(x => x.FlowSteps)
                .HasForeignKey(x => x.SubFlowId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            // Relationship with FlowSearchArea (one-to-many)
            builder.HasOne(x => x.FlowSearchArea)
                .WithMany(x => x.FlowSteps)
                .HasForeignKey(x => x.FlowSearchAreaId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            // Relationship with General FlowStep reference (one-to-many)
            builder.HasOne(x => x.FlowStepReference)
                .WithMany(x => x.FlowStepReferences)
                .HasForeignKey(x => x.FlowStepReferenceId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed
        }
    }
}
