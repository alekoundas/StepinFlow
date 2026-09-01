using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record ExplainExecutionQuery(int executionId) : IRequest<ResultDto<AiAnswerDto>>;
    public record GetAiStatusQuery() : IRequest<ResultDto<bool>>;
    public record GetAiModelsQuery() : IRequest<ResultDto<AiModelsDto>>;
    public record GetAiModelSuggestionsQuery() : IRequest<ResultDto<IReadOnlyList<AiModelSuggestionDto>>>;


    // ============== COMMANDS ==============
    public record PullAiModelCommand(string model) : IRequest<ResultDto<bool>>;
    public record GetAiPullStateQuery() : IRequest<ResultDto<AiModelPullEventDto?>>;
    public record ClearAiPullStateCommand() : IRequest<ResultDto<bool>>;
}
