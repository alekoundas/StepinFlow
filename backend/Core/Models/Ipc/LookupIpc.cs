using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetLookupWindowQuery(LookupRequestDto Dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupMonitorQuery(LookupRequestDto Dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupFlowStepQuery(LookupRequestDto Dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupFlowPointQuery(LookupRequestDto Dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupFlowAreaQuery(LookupRequestDto Dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupSubFlowQuery(LookupRequestDto Dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupDiscordBotQuery(LookupRequestDto Dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupFailedStepQuery(LookupRequestDto Dto) : IRequest<ResultDto<LookupResponseDto>>;

    /// <summary>What a window matcher finds right now, so a typo is caught while it is typed.</summary>
    public record TestWindowMatchQuery(WindowMatchTestRequestDto Dto) : IRequest<ResultDto<WindowMatchTestResultDto>>;
    public record GetLookupCommandPresetsQuery() : IRequest<ResultDto<IReadOnlyList<CommandPresetDto>>>;
    public record GetLookupOcrLanguagesQuery() : IRequest<ResultDto<IReadOnlyList<OcrLanguageDto>>>;
    public record GetLookupAiModelsQuery() : IRequest<ResultDto<AiModelsDto>>;
    public record GetLookupAiModelSuggestionsQuery() : IRequest<ResultDto<IReadOnlyList<AiModelSuggestionDto>>>;


    // ============== COMMANDS ==============
}
