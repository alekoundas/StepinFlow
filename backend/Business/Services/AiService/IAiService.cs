using Core.Models.Dtos;

namespace Business.Services.AiService
{
    public interface IAiService
    {
        /// <summary>Whether an AI provider is set up, so the UI can offer the feature or not.</summary>
        Task<bool> IsConfiguredAsync(CancellationToken ct = default);

        /// <summary>What the chosen provider can be asked for, so the setting is a list and not a guess.</summary>
        Task<AiModelsDto> GetModelsAsync(CancellationToken ct = default);

        /// <summary>Local models worth offering, with the ones already pulled marked.</summary>
        Task<IReadOnlyList<AiModelSuggestionDto>> GetModelSuggestionsAsync(CancellationToken ct = default);

        /// <summary>
        /// Starts a download and returns. Gigabytes and minutes, so progress arrives on the
        /// broadcast pipe rather than on this call.
        ///
        /// Here rather than on the download service because it is the settings that decide whether
        /// there is anything to download from: reading it is the work. Asking how one is going, and
        /// forgetting a finished one, are the download service's own business and go straight to it.
        /// </summary>
        Task<bool> StartModelPullAsync(string model, CancellationToken ct = default);

        /// <summary>Reads a finished run and says, in a paragraph, what went wrong and why.</summary>
        Task<AiAnswerDto> ExplainExecutionAsync(int executionId, CancellationToken ct = default);
    }
}
