using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowStepImageConfiguration : IEntityTypeConfiguration<FlowStepImage>
    {
        public void Configure(EntityTypeBuilder<FlowStepImage> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TemplateMatchMode).HasConversion<string>();
            builder.Property(x => x.ClickAnchor).HasConversion<string>();


            // Relationship with FlowStep (one-to-many)
            builder.HasOne(x => x.FlowStep)
                .WithMany(x => x.FlowStepImages  )
                .HasForeignKey(x => x.FlowStepId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed
        }
    }
}
