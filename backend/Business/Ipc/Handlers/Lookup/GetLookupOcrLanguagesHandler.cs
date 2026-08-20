using Business.Services.OcrService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class GetLookupOcrLanguagesHandler : IRequestHandler<GetLookupOcrLanguagesQuery, ResultDto<IReadOnlyList<OcrLanguageDto>>>
    {
        private readonly IOcrService _ocrService;

        public GetLookupOcrLanguagesHandler(IOcrService ocrService)
        {
            _ocrService = ocrService;
        }

        public Task<ResultDto<IReadOnlyList<OcrLanguageDto>>> Handle(GetLookupOcrLanguagesQuery request, CancellationToken ct) =>
            Task.FromResult(ResultDto<IReadOnlyList<OcrLanguageDto>>.Success(_ocrService.GetLanguages()));
    }
}
