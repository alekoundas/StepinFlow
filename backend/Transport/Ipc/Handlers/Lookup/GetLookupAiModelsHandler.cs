using Business.Ai.AiModels;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Lookup
{
    /// <summary>What the chosen provider offers, so the model setting is a list and not a guess.</summary>
    public class GetLookupAiModelsHandler
    {
        private readonly IAiModelService _modelService;

        public GetLookupAiModelsHandler(IAiModelService modelService)
        {
            _modelService = modelService;
        }

        public async Task<ResultDto<AiModelsDto>> HandleAsync(CancellationToken ct)
        {
            AiModelsDto models = await _modelService.GetModelsAsync(ct);
            return ResultDto<AiModelsDto>.Success(models);
        }
    }
}
