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


            // Relationship with Flow (one-to-many)
            builder.HasOne(x => x.Flow)
                .WithMany(x => x.FlowLocations)
                .HasForeignKey(x => x.FlowId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            // Lookups are always scoped to a Flow.
            builder.HasIndex(x => x.FlowId);
        }
    }
}
