using Core.Ports;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SystemInstallOcrLanguageHandler
    {
        private readonly IOcrService _ocrService;

        public SystemInstallOcrLanguageHandler(IOcrService ocrService)
        {
            _ocrService = ocrService;
        }

        public async Task<ResultDto<OcrLanguageInstallResultDto>> HandleAsync(string languageTag, CancellationToken ct)
        {
            OcrLanguageInstallResultDto result = await _ocrService.InstallLanguageAsync(languageTag, ct);

            return result.ErrorMessage == null
                ? ResultDto<OcrLanguageInstallResultDto>.Success(result)
                : ResultDto<OcrLanguageInstallResultDto>.Failure(result.ErrorMessage);
        }
    }
}
