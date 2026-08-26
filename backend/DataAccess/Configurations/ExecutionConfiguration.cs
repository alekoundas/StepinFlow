using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class ExecutionConfiguration : IEntityTypeConfiguration<Execution>
    {
        public void Configure(EntityTypeBuilder<Execution> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).HasConversion<string>();
            builder.Property(x => x.HistoryLevel).HasConversion<string>();

            // History for a flow that no longer exists is not worth keeping.
            builder.HasOne(x => x.Flow)
                .WithMany()
                .HasForeignKey(x => x.FlowId)
                .OnDelete(DeleteBehavior.Cascade);

            // Every list of runs is "this flow, newest first".
            builder.HasIndex(x => new { x.FlowId, x.CreatedOn });
        }
    }
}
