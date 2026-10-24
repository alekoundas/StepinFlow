using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowTemplateConfiguration : IEntityTypeConfiguration<FlowTemplate>
    {
        public void Configure(EntityTypeBuilder<FlowTemplate> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.FlowId);


            // Relationship with Flow (one-to-many)
            builder.HasOne(x => x.Flow)
                .WithMany(x => x.FlowTemplates)
                .HasForeignKey(x => x.FlowId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed
        }
    }
}
