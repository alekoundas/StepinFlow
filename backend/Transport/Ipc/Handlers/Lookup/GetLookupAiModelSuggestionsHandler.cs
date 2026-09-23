using Business.Services.Ai.AiModels;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Lookup
{
    /// <summary>Local models worth offering, with the ones already downloaded marked.</summary>
    public class GetLookupAiModelSuggestionsHandler
    {
        private readonly IAiModelService _modelService;

        public GetLookupAiModelSuggestionsHandler(IAiModelService modelService)
        {
            _modelService = modelService;
        }

        public async Task<ResultDto<IReadOnlyList<AiModelSuggestionDto>>> HandleAsync(CancellationToken ct)
        {
            IReadOnlyList<AiModelSuggestionDto> suggestions = await _modelService.GetModelSuggestionsAsync(ct);
            return ResultDto<IReadOnlyList<AiModelSuggestionDto>>.Success(suggestions);
        }
    }
}
