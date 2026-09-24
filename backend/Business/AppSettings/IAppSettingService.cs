using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Services.AppSettingService
{
    public interface IAppSettingService
    {
        /// <summary>Reads a setting, falling back to the catalog default when it was never set.</summary>
        Task<int> GetAsync(IntAppSettingDefinition definition, CancellationToken ct = default);

        /// <summary>The same, for settings whose value is text rather than a number.</summary>
        Task<string> GetTextAsync(AppSettingDefinition definition, CancellationToken ct = default);

        /// <summary>Every setting with its definition and current value, for the Settings page.</summary>
        Task<IReadOnlyList<AppSettingDto>> GetAllAsync(CancellationToken ct = default);

        Task SetAsync(AppSettingKeyEnum key, string value, CancellationToken ct = default);
    }
}
