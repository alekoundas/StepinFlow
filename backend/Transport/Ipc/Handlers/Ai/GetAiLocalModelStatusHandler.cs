using Business.Ai.AiModels;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Ai
{
    /// <summary>Where the local model stands: downloaded, in memory, and how much memory it takes.</summary>
    public class GetAiLocalModelStatusHandler
    {
        private readonly IAiModelService _modelService;

        public GetAiLocalModelStatusHandler(IAiModelService modelService)
        {
            _modelService = modelService;
        }

        public async Task<ResultDto<AiLocalModelStatusDto>> HandleAsync(CancellationToken ct)
        {
            AiLocalModelStatusDto status = await _modelService.GetLocalModelStatusAsync(ct);
            return ResultDto<AiLocalModelStatusDto>.Success(status);
        }
    }
}
