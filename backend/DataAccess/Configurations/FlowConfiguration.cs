using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowConfiguration : IEntityTypeConfiguration<Flow>
    {
        public void Configure(EntityTypeBuilder<Flow> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AppCloseMode).HasConversion<string>();


            // Relationship with FlowArea (one-to-one, the application under test)
            // NoAction rather than a cascade: the area already cascades from this flow, and a
            // second path to the same rows is a cycle SQLite will not take.
            builder.HasOne(x => x.AppUnderTestArea)
                .WithMany()
                .HasForeignKey(x => x.AppUnderTestAreaId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
