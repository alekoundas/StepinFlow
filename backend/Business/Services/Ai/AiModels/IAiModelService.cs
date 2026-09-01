using Core.Models.Dtos;

namespace Business.Services.Ai.AiModels
{
    public interface IAiModelService
    {
        Task<AiModelsDto> GetModelsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<AiModelSuggestionDto>> GetModelSuggestionsAsync(CancellationToken ct = default);
        Task<bool> StartModelDownloadAsync(string model, CancellationToken ct = default);
        Task<bool> SupportsToolsAsync(CancellationToken ct = default);
    }
}
