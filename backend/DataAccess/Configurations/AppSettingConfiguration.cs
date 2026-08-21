using Core.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations
{
    public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
    {
        public void Configure(EntityTypeBuilder<AppSetting> builder)
        {
            // The key is the identity: one row per setting, or none while it is still default.
            builder.HasKey(x => x.Key);

            builder.Property(x => x.Key).HasConversion<string>();
        }
    }
}
