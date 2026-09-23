using Core.Ports;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SystemOpenWindowsLanguageSettingsHandler
    {
        private readonly IOcrService _ocrService;

        public SystemOpenWindowsLanguageSettingsHandler(IOcrService ocrService)
        {
            _ocrService = ocrService;
        }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            _ocrService.OpenWindowsLanguageSettings();
            return Task.FromResult(ResultDto<bool>.Success(true));
        }
    }
}
