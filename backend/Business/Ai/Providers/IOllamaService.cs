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
    }
}
