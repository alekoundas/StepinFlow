using Core.Models.Dtos;

namespace Business.Ai.AiModels
{
    public interface IAiModelService
    {
        Task<AiModelsDto> GetModelsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<AiModelSuggestionDto>> GetModelSuggestionsAsync(CancellationToken ct = default);
        Task<bool> StartModelDownloadAsync(string model, CancellationToken ct = default);
        Task<bool> SupportsToolsAsync(CancellationToken ct = default);
        Task<bool> SupportsVisionAsync(CancellationToken ct = default);
        Task<AiLocalModelStatusDto> GetLocalModelStatusAsync(CancellationToken ct = default);

        /// <summary>Tells Ollama to let the local model go. Only the status can say it has.</summary>
        Task<bool> FreeLocalModelAsync(CancellationToken ct = default);
    }
}
