using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowSearchAreaConfiguration : IEntityTypeConfiguration<FlowSearchArea>
    {
        public void Configure(EntityTypeBuilder<FlowSearchArea> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);



            // Relationship with Flow (one-to-many)
            builder.HasOne(x => x.Flow)
                .WithMany(x => x.FlowSearchAreas)
                .HasForeignKey(x => x.FlowId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed
        }
    }
}
