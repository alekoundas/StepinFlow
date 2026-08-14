using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowPointConfiguration : IEntityTypeConfiguration<FlowPoint>
    {
        public void Configure(EntityTypeBuilder<FlowPoint> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OffsetMode).HasConversion<string>();


            // Relationship with Flow (one-to-many)
            builder.HasOne(x => x.Flow)
                .WithMany(x => x.FlowPoints)
                .HasForeignKey(x => x.FlowId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            // Frame the point is measured from. Losing it makes the point absolute, not invalid.
            builder.HasOne(x => x.FlowArea)
                .WithMany(x => x.FlowPoints)
                .HasForeignKey(x => x.FlowAreaId)
                .OnDelete(DeleteBehavior.SetNull);


            builder.HasIndex(x => x.FlowId);
        }
    }
}
