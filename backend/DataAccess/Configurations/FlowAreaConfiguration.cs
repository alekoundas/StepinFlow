using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowAreaConfiguration : IEntityTypeConfiguration<FlowArea>
    {
        public void Configure(EntityTypeBuilder<FlowArea> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type).HasConversion<string>();
            builder.Property(x => x.SizingMode).HasConversion<string>();
            builder.Property(x => x.TitleMatchMode).HasConversion<string>();
            builder.Property(x => x.TabMatchOn).HasConversion<string>();


            // Relationship with Flow (one-to-many)
            builder.HasOne(x => x.Flow)
                .WithMany(x => x.FlowAreas)
                .HasForeignKey(x => x.FlowId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            // Relationship with ParentFlowArea (one-to-many)
            builder.HasOne(x => x.ParentFlowArea)
                .WithMany(x => x.ChildFlowAreas)
                .HasForeignKey(x => x.ParentFlowAreaId)
                .OnDelete(DeleteBehavior.SetNull);


            builder.HasIndex(x => x.FlowId);
            builder.HasIndex(x => x.ParentFlowAreaId);
        }
    }
}
