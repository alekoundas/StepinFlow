using Core.Enums;
using Microsoft.Extensions.AI;

namespace Business.Services.AiService
{
    public interface IChatClientFactory
    {
        /// <summary>Which provider is chosen. NONE when the features are switched off.</summary>
        Task<AiProviderEnum> GetProviderAsync(CancellationToken ct = default);

        /// <summary>Whether a provider is set up. Every feature checks this before offering itself.</summary>
        Task<bool> IsConfiguredAsync(CancellationToken ct = default);

        /// <summary>The client for whatever provider is set, or null when there is none.</summary>
        Task<IChatClient?> CreateAsync(CancellationToken ct = default);
    }
}
