using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class FlowConfiguration : IEntityTypeConfiguration<Flow>
    {
        public void Configure(EntityTypeBuilder<Flow> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Id).IsUnique();

            // A deleted flow keeps its Id until the cleanup. (Add filter to allow branch switch or import the same flow again)
            builder.HasIndex(x => x.PublicId).IsUnique().HasFilter("\"IsDeleted\" = 0");

        }
    }
}
