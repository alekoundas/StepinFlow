using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowLocationConfiguration : IEntityTypeConfiguration<FlowLocation>
    {
        public void Configure(EntityTypeBuilder<FlowLocation> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Anchor).HasConversion<string>();
            builder.Property(x => x.OffsetMode).HasConversion<string>();


            // Relationship with Flow (one-to-many)
            builder.HasOne(x => x.Flow)
                .WithMany(x => x.FlowLocations)
                .HasForeignKey(x => x.FlowId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            // Frame the point is measured from. Losing it makes the point absolute, not invalid.
            builder.HasOne(x => x.FlowSearchArea)
                .WithMany(x => x.FlowLocations)
                .HasForeignKey(x => x.FlowSearchAreaId)
                .OnDelete(DeleteBehavior.SetNull);


            builder.HasIndex(x => x.FlowId);
        }
    }
}
