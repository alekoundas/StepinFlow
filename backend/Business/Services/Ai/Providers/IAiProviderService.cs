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

        /// <summary>
        /// Whether a cloud provider may be shown what was on screen - text read by OCR, and the
        /// screenshots a run kept. A local model is not covered by this: nothing leaves the machine.
        /// </summary>
        Task<bool> IsScreenContentAllowedAsync(CancellationToken ct = default);
    }
}
