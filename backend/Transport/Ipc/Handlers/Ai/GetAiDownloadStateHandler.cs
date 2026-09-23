using Business.Services.Ai.AiModels;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Ai
{
    /// <summary>How the current or last download is going, for a page that has just opened.</summary>
    public class GetAiDownloadStateHandler
    {
        private readonly IAiModelDownloadService _downloadService;

        public GetAiDownloadStateHandler(IAiModelDownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public Task<ResultDto<AiModelDownloadEventDto?>> HandleAsync(CancellationToken ct)
        {
            return Task.FromResult(ResultDto<AiModelDownloadEventDto?>.Success(_downloadService.Current));
        }
    }
}
