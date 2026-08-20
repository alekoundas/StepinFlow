using Business.Services.OcrService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class SystemOpenWindowsLanguageSettingsHandler : IRequestHandler<SystemOpenWindowsLanguageSettingsCommand, ResultDto<bool>>
    {
        private readonly IOcrService _ocrService;

        public SystemOpenWindowsLanguageSettingsHandler(IOcrService ocrService)
        {
            _ocrService = ocrService;
        }

        public Task<ResultDto<bool>> Handle(SystemOpenWindowsLanguageSettingsCommand request, CancellationToken ct)
        {
            _ocrService.OpenWindowsLanguageSettings();
            return Task.FromResult(ResultDto<bool>.Success(true));
        }
    }
}
