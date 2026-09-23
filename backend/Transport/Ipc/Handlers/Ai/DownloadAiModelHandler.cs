using Business.Services.Ai.AiModels;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Ai
{
    /// <summary>
    /// Starts a download and comes straight back. It runs for minutes, so how it is going arrives
    /// on the broadcast pipe rather than on this call.
    /// </summary>
    public class DownloadAiModelHandler
    {
        private readonly IAiModelService _modelService;

        public DownloadAiModelHandler(IAiModelService modelService)
        {
            _modelService = modelService;
        }

        public async Task<ResultDto<bool>> HandleAsync(string model, CancellationToken ct)
        {
            bool isStarted = await _modelService.StartModelDownloadAsync(model, ct);

            if (!isStarted)
                return ResultDto<bool>.Failure("Downloading a model needs Ollama to be the chosen provider.");

            return ResultDto<bool>.Success(true);
        }
    }
}
