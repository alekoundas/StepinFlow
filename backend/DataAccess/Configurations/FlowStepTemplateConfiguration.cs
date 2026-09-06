using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowStepTemplateConfiguration : IEntityTypeConfiguration<FlowStepTemplate>
    {
        public void Configure(EntityTypeBuilder<FlowStepTemplate> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TemplateMatchMode).HasConversion<string>();


            // Relationship with FlowStep (one-to-many)
            builder.HasOne(x => x.FlowStep)
                .WithMany(x => x.FlowStepTemplates)
                .HasForeignKey(x => x.FlowStepId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed
        }
    }
}
