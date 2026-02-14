using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowStepImageConfiguration : IEntityTypeConfiguration<FlowStepImage>
    {
        public void Configure(EntityTypeBuilder<FlowStepImage> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);


            // Relationship with FlowStep (one-to-many)
            builder.HasOne(x => x.FlowStep)
                .WithMany(x => x.FlowStepImages  )
                .HasForeignKey(x => x.FlowStepId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed
        }
    }
}
