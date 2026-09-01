using Core.Enums;
using Core.Models.Business;

namespace Business.Services.Ai.Providers
{
    public interface IAiProviderService
    {
        Task<AiProviderEnum> GetProviderAsync(CancellationToken ct = default);
        Task<bool> IsConfiguredAsync(CancellationToken ct = default);
        Task<AiSettings?> ReadAsync(CancellationToken ct = default);
        Task<string> GetModelAsync(CancellationToken ct = default);
        Task<string> GetOllamaUrlAsync(CancellationToken ct = default);
    }
}
