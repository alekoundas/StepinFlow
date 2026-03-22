using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class ExecutionStepConfiguration : IEntityTypeConfiguration<ExecutionStep>
    {
        public void Configure(EntityTypeBuilder<ExecutionStep> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);


            builder.HasOne(x => x.Execution)
                .WithMany(x => x.ExecutionSteps)
                .HasForeignKey(x => x.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade); // Delete if parent is removed
        }
    }
}
