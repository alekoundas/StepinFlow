using Core.Ports;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class GetLookupOcrLanguagesHandler
    {
        private readonly IOcrService _ocrService;

        public GetLookupOcrLanguagesHandler(IOcrService ocrService)
        {
            _ocrService = ocrService;
        }

        public Task<ResultDto<IReadOnlyList<OcrLanguageDto>>> HandleAsync(CancellationToken ct)
        {
            return Task.FromResult(ResultDto<IReadOnlyList<OcrLanguageDto>>.Success(_ocrService.GetLanguages()));
        }
    }
}
