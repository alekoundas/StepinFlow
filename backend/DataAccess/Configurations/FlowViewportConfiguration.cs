using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowViewportConfiguration : IEntityTypeConfiguration<FlowViewport>
    {
        public void Configure(EntityTypeBuilder<FlowViewport> builder)
        {
            builder.HasKey(x => x.Id);


            // Relationship with Flow (one-to-many)
            builder.HasOne(x => x.Flow)
                .WithMany(x => x.FlowViewports)
                .HasForeignKey(x => x.FlowId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            builder.HasIndex(x => new { x.FlowId, x.OrderNumber });
        }
    }
}
