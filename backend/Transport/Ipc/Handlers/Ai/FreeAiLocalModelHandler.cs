using Business.Ai.AiModels;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Ai
{
    /// <summary>Tells Ollama to let the local model go. Whether it has is the status's to say.</summary>
    public class FreeAiLocalModelHandler
    {
        private readonly IAiModelService _modelService;

        public FreeAiLocalModelHandler(IAiModelService modelService)
        {
            _modelService = modelService;
        }

        public async Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            bool isAgreed = await _modelService.FreeLocalModelAsync(ct);
            return ResultDto<bool>.Success(isAgreed);
        }
    }
}
