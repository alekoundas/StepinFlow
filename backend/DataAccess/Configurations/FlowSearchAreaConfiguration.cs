using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowSearchAreaConfiguration : IEntityTypeConfiguration<FlowSearchArea>
    {
        public void Configure(EntityTypeBuilder<FlowSearchArea> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type).HasConversion<string>();
            builder.Property(x => x.SizingMode).HasConversion<string>();
            builder.Property(x => x.TitleMatchMode).HasConversion<string>();
            builder.Property(x => x.BrowserType).HasConversion<string>();
            builder.Property(x => x.TabMatchOn).HasConversion<string>();


            // Relationship with Flow (one-to-many)
            builder.HasOne(x => x.Flow)
                .WithMany(x => x.FlowSearchAreas)
                .HasForeignKey(x => x.FlowId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            // A CUSTOM area may sit inside another area. Removing the frame leaves its regions
            // behind as absolute rather than deleting work the user may still want.
            builder.HasOne(x => x.ParentFlowSearchArea)
                .WithMany(x => x.ChildFlowSearchAreas)
                .HasForeignKey(x => x.ParentFlowSearchAreaId)
                .OnDelete(DeleteBehavior.SetNull);


            builder.HasIndex(x => x.FlowId);
            builder.HasIndex(x => x.ParentFlowSearchAreaId);
        }
    }
}
