using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetLookupWindowQuery(LookupRequestDto dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupMonitorQuery(LookupRequestDto dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupFlowStepQuery(LookupRequestDto dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupFlowPointQuery(LookupRequestDto dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupFlowAreaQuery(LookupRequestDto dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupSubFlowQuery(LookupRequestDto dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupCommandPresetsQuery() : IRequest<ResultDto<IReadOnlyList<CommandPresetDto>>>;
    public record GetLookupOcrLanguagesQuery() : IRequest<ResultDto<IReadOnlyList<OcrLanguageDto>>>;


    // ============== COMMANDS ==============
}
