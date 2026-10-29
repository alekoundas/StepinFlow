using Core.Models.Business;

namespace Business.Ai.Providers
{
    public interface IAiProviderService
    {
        Task<bool> IsConfiguredAsync(CancellationToken ct = default);
        Task<AiSettings?> ReadAsync(CancellationToken ct = default);

        /// <summary>The model that answers: the cloud one once it has a model and a key, the local one otherwise.</summary>
        Task<string> GetModelAsync(CancellationToken ct = default);

        /// <summary>Whether a cloud model answers in the local one's place.</summary>
        Task<bool> IsCloudModelInUseAsync(CancellationToken ct = default);

        Task<string> GetOllamaModelAsync(CancellationToken ct = default);
        Task<string> GetOllamaUrlAsync(CancellationToken ct = default);

        /// <summary>
        /// A local model always may: nothing leaves the machine.
        /// A cloud model may only when the setting says so, and it is off until someone turns it on.
        /// </summary>
        Task<bool> CanSendScreenDataAsync(CancellationToken ct = default);

        /// <summary>How big a context window to ask Ollama for.</summary>
        Task<int> GetOllamaContextLengthAsync(CancellationToken ct = default);
    }
}
