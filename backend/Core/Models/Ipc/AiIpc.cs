using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record ExplainExecutionQuery(int executionId) : IRequest<ResultDto<AiAnswerDto>>;
    public record GetAiStatusQuery() : IRequest<ResultDto<bool>>;
    public record GetAiChatAvailabilityQuery() : IRequest<ResultDto<AiChatAvailabilityDto>>;
    public record AskAiQuery(AiChatRequestDto dto) : IRequest<ResultDto<AiChatAnswerDto>>;


    // ============== COMMANDS ==============
    public record DownloadAiModelCommand(string model) : IRequest<ResultDto<bool>>;
    public record GetAiDownloadStateQuery() : IRequest<ResultDto<AiModelDownloadEventDto?>>;
    public record ClearAiDownloadStateCommand() : IRequest<ResultDto<bool>>;
}
