using Core.Models.Dtos;

namespace Business.Ai.Providers
{
    public interface IOllamaService
    {
        /// <summary>The models downloaded onto this machine, or null when Ollama does not answer.</summary>
        Task<IReadOnlyList<string>?> ListModelsAsync(string baseUrl, CancellationToken ct = default);

        /// <summary>Whether Ollama answers and has this model downloaded.</summary>
        Task<bool> HasModelAsync(string baseUrl, string model, CancellationToken ct = default);

        /// <summary>What one model can do. A model that will not answer still comes back, without capabilities.</summary>
        Task<AiModelDto> DescribeAsync(string baseUrl, string model, CancellationToken ct = default);

        /// <summary>The models in memory right now, or null when Ollama does not answer.</summary>
        Task<IReadOnlyList<AiLoadedModelDto>?> ListLoadedModelsAsync(string baseUrl, CancellationToken ct = default);

        /// <summary>
        /// Tells Ollama to let a model go. True means it agreed, not that the model has gone: a busy
        /// one finishes what it is doing first, so only <see cref="ListLoadedModelsAsync"/> can confirm it.
        /// </summary>
        Task<bool> UnloadAsync(string baseUrl, string model, CancellationToken ct = default);
    }
}
