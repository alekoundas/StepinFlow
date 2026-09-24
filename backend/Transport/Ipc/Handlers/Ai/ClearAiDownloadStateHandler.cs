using Business.Ai.AiModels;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Ai
{
    /// <summary>Dismisses a finished download. A running one stays, because dismissing it would be a lie.</summary>
    public class ClearAiDownloadStateHandler
    {
        private readonly IAiModelDownloadService _downloadService;

        public ClearAiDownloadStateHandler(IAiModelDownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            _downloadService.Clear();
            return Task.FromResult(ResultDto<bool>.Success(true));
        }
    }
}
