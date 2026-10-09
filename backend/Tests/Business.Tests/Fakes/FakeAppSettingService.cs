using Business.AppSettings;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Tests.Fakes
{
    /// <summary>Every setting at its catalog default.</summary>
    public sealed class FakeAppSettingService : IAppSettingService
    {
        public Task<int> GetAsync(IntAppSettingDefinition definition, CancellationToken ct = default)
        {
            return Task.FromResult(definition.DefaultValue);
        }

        public Task<string> GetTextAsync(AppSettingDefinition definition, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<AppSettingDto>> GetAllAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(AppSettingKeyEnum key, string value, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
