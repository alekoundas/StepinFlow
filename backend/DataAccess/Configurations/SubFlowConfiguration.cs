using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class SubFlowConfiguration : IEntityTypeConfiguration<SubFlow>
    {
        public void Configure(EntityTypeBuilder<SubFlow> builder)
        {
            builder.HasIndex(x => x.Id).IsUnique();
            builder.HasKey(x => x.Id);
        }
    }
}
