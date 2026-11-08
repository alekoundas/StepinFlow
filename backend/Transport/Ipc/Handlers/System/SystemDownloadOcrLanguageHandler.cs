using Business.ScreenText;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SystemDownloadOcrLanguageHandler
    {
        private readonly IOcrReaderDownloadService _downloadService;

        public SystemDownloadOcrLanguageHandler(IOcrReaderDownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public async Task<ResultDto<bool>> HandleAsync(string language, CancellationToken ct)
        {
            string? error = await _downloadService.DownloadAsync(language, ct);
            if (error != null)
                return ResultDto<bool>.Failure(error);

            return ResultDto<bool>.Success(true);
        }
    }
}
