using Business.Services.OcrService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class SystemInstallOcrLanguageHandler : IRequestHandler<SystemInstallOcrLanguageCommand, ResultDto<OcrLanguageInstallResultDto>>
    {
        private readonly IOcrService _ocrService;

        public SystemInstallOcrLanguageHandler(IOcrService ocrService)
        {
            _ocrService = ocrService;
        }

        public async Task<ResultDto<OcrLanguageInstallResultDto>> Handle(SystemInstallOcrLanguageCommand request, CancellationToken ct)
        {
            OcrLanguageInstallResultDto result = await _ocrService.InstallLanguageAsync(request.languageTag, ct);

            return result.ErrorMessage == null
                ? ResultDto<OcrLanguageInstallResultDto>.Success(result)
                : ResultDto<OcrLanguageInstallResultDto>.Failure(result.ErrorMessage);
        }
    }
}
