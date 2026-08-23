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
    public record GetLookupDiscordBotQuery(LookupRequestDto dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupFailedStepQuery(LookupRequestDto dto) : IRequest<ResultDto<LookupResponseDto>>;

    /// <summary>What a window matcher finds right now, so a typo is caught while it is typed.</summary>
    public record TestWindowMatchQuery(WindowMatchTestRequestDto dto) : IRequest<ResultDto<WindowMatchTestResultDto>>;
    public record GetLookupCommandPresetsQuery() : IRequest<ResultDto<IReadOnlyList<CommandPresetDto>>>;
    public record GetLookupOcrLanguagesQuery() : IRequest<ResultDto<IReadOnlyList<OcrLanguageDto>>>;


    // ============== COMMANDS ==============
}
