using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowInputConfiguration : IEntityTypeConfiguration<FlowInput>
    {
        public void Configure(EntityTypeBuilder<FlowInput> builder)
        {
            builder.HasKey(x => x.Id);


            // Relationship with Flow (one-to-many)
            builder.HasOne(x => x.Flow)
                .WithMany(x => x.FlowInputs)
                .HasForeignKey(x => x.FlowId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed


            // The name is what a step writes as {{name}}, so two of them make it ambiguous.
            builder.HasIndex(x => new { x.FlowId, x.Name }).IsUnique();
        }
    }
}
