using Business.AppSettings;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Tests.Fakes
{
    /// <summary>Every setting at its catalog default, until a test or the code sets it.</summary>
    public sealed class FakeAppSettingService : IAppSettingService
    {
        public Dictionary<AppSettingKeyEnum, string> Stored { get; } = new Dictionary<AppSettingKeyEnum, string>();

        public Task<int> GetAsync(IntAppSettingDefinition definition, CancellationToken ct = default)
        {
            return Task.FromResult(definition.DefaultValue);
        }

        public Task<string> GetTextAsync(AppSettingDefinition definition, CancellationToken ct = default)
        {
            if (Stored.TryGetValue(definition.Key, out string? value))
                return Task.FromResult(value);

            return Task.FromResult(definition.DefaultAsText);
        }

        public Task<IReadOnlyList<AppSettingDto>> GetAllAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(AppSettingKeyEnum key, string value, CancellationToken ct = default)
        {
            Stored[key] = value;

            return Task.CompletedTask;
        }
    }
}
