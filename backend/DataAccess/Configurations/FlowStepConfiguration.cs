using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowStepConfiguration : IEntityTypeConfiguration<FlowStep>
    {
        public void Configure(EntityTypeBuilder<FlowStep> builder)
        {
            builder.HasKey(x => x.Id);

            // Indexes for the queries the tree and the executor run constantly.
            builder.HasIndex(x => x.RootId);
            builder.HasIndex(x => new { x.FlowId, x.OrderNumber });
            builder.HasIndex(x => new { x.ParentFlowStepId, x.OrderNumber });


            // Properties - Store enum as string

            builder.Property(x => x.FlowStepType).HasConversion<string>();
            builder.Property(x => x.ConditionType).HasConversion<string>();
            builder.Property(x => x.CursorButtonType).HasConversion<string>();
            builder.Property(x => x.CursorButtonActionType).HasConversion<string>();
            builder.Property(x => x.CursorScrollDirectionType).HasConversion<string>();
            builder.Property(x => x.KeyboardInputType).HasConversion<string>();
            builder.Property(x => x.TitleMatchMode).HasConversion<string>();
            builder.Property(x => x.SearchMode).HasConversion<string>();
            builder.Property(x => x.TemplateMatchMode).HasConversion<string>();
            builder.Property(x => x.RunCommandShell).HasConversion<string>();
            builder.Property(x => x.RunCommandPreset).HasConversion<string>();
            builder.Property(x => x.ResultSource).HasConversion<string>();
            builder.Property(x => x.SystemActionType).HasConversion<string>();


            // Relationship with Flow (one-to-many)
            builder.HasOne(x => x.Flow)
                .WithMany(x => x.FlowSteps  )
                .HasForeignKey(x => x.FlowId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            // The flow a SUB_FLOW step runs. A reference, not ownership: deleting the invoked
            // flow clears the reference and the validator reports it, the same as a deleted area.
            builder.HasOne(x => x.InvokedFlow)
                .WithMany()
                .HasForeignKey(x => x.InvokedFlowId)
                .OnDelete(DeleteBehavior.SetNull);


            // Relationship with Parent FlowStep (one-to-many)
            builder.HasOne(x => x.ParentFlowStep)
                .WithMany(x => x.ChildrenFlowSteps)
                .HasForeignKey(x => x.ParentFlowStepId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            // Relationship with FlowArea (one-to-many)
            // A search area is reusable, so removing it must not take the steps using it down with it.
            builder.HasOne(x => x.FlowArea)
                .WithMany(x => x.FlowSteps)
                .HasForeignKey(x => x.FlowAreaId)
                .OnDelete(DeleteBehavior.SetNull); // Only clear the reference


            // Relationship with FlowPoint, start point (one-to-many)
            builder.HasOne(x => x.FlowPoint)
                .WithMany(x => x.FlowSteps)
                .HasForeignKey(x => x.FlowPointId)
                .OnDelete(DeleteBehavior.SetNull); // Only clear the reference


            // Relationship with FlowPoint, end point (one-to-many)
            builder.HasOne(x => x.FlowPointEnd)
                .WithMany(x => x.EndFlowSteps)
                .HasForeignKey(x => x.FlowPointEndId)
                .OnDelete(DeleteBehavior.SetNull); // Only clear the reference


            // Relationship with General FlowStep reference, start point (one-to-many)
            builder.HasOne(x => x.FlowStepReference)
                .WithMany(x => x.FlowStepReferences)
                .HasForeignKey(x => x.FlowStepReferenceId)
                .OnDelete(DeleteBehavior.SetNull); // Only clear the reference


            // Relationship with General FlowStep reference, end point (one-to-many)
            builder.HasOne(x => x.FlowStepReferenceEnd)
                .WithMany(x => x.FlowStepReferencesEnd)
                .HasForeignKey(x => x.FlowStepReferenceEndId)
                .OnDelete(DeleteBehavior.SetNull); // Only clear the reference


          
        }
    }
}
