using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Services.AppSettingService
{
    public sealed class AppSettingService : IAppSettingService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public AppSettingService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<int> GetAsync(IntAppSettingDefinition definition, CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            string? stored = await dbContext.AppSettings
                .AsNoTracking()
                .Where(x => x.Key == definition.Key)
                .Select(x => x.Value)
                .FirstOrDefaultAsync(ct);

            return definition.Parse(stored);
        }

        public async Task<string> GetTextAsync(AppSettingDefinition definition, CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            string? stored = await dbContext.AppSettings
                .AsNoTracking()
                .Where(x => x.Key == definition.Key)
                .Select(x => x.Value)
                .FirstOrDefaultAsync(ct);

            return string.IsNullOrWhiteSpace(stored) ? definition.DefaultAsText : stored;
        }

        public async Task<IReadOnlyList<AppSettingDto>> GetAllAsync(CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            Dictionary<AppSettingKeyEnum, string> stored = await dbContext.AppSettings
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Key, x => x.Value, ct);

            return AppSettingCatalog.All
                .Select(definition => new AppSettingDto
                {
                    Key = definition.Key,
                    Kind = definition.Kind,
                    Label = definition.Label,
                    Description = definition.Description,
                    Value = stored.TryGetValue(definition.Key, out string? value) ? value : definition.DefaultAsText,
                    Minimum = definition.Minimum,
                    Maximum = definition.Maximum,
                })
                .ToList();
        }

        public async Task SetAsync(AppSettingKeyEnum key, string value, CancellationToken ct = default)
        {
            // Normalised through the definition, so a value that arrives out of range is stored
            // clamped rather than re-clamped on every read for the rest of its life.
            if (AppSettingCatalog.Find(key) is IntAppSettingDefinition numeric)
                value = numeric.ToText(numeric.Parse(value));

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            AppSetting? existing = await dbContext.AppSettings.FirstOrDefaultAsync(x => x.Key == key, ct);

            if (existing == null)
                dbContext.AppSettings.Add(new AppSetting { Key = key, Value = value });
            else
                existing.Value = value;

            await dbContext.SaveChangesAsync(ct);
        }
    }
}
