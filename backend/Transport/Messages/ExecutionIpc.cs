using Core.Models.Dtos;
using Core.Models.Dtos.Database;
using MediatR;

namespace Transport.Messages
{
    // ============== COMMANDS ==============
    public record StartExecutionCommand(ExecutionStartDto Dto) : IRequest<ResultDto<int>>;
    public record StopExecutionCommand() : IRequest<ResultDto<bool>>;
    public record PauseExecutionCommand() : IRequest<ResultDto<bool>>;
    public record ContinueExecutionCommand() : IRequest<ResultDto<bool>>;
    public record StepIntoExecutionCommand() : IRequest<ResultDto<bool>>;
    public record StepOverExecutionCommand() : IRequest<ResultDto<bool>>;
    public record SetExecutionBreakpointsCommand(List<int> FlowStepIds) : IRequest<ResultDto<bool>>;


    // ============== QUERIES ==============
    public record GetExecutionQuery(int Id) : IRequest<ResultDto<ExecutionDto>>;
    public record GetExecutionListQuery(int FlowId) : IRequest<ResultDto<List<ExecutionDto>>>;
    public record GetExecutionStateQuery() : IRequest<ResultDto<ExecutionStateDto>>;
    public record GetFlowExecutionSummariesQuery() : IRequest<ResultDto<List<FlowExecutionSummaryDto>>>;
    public record GetExecutionStepScreenshotQuery(int ExecutionStepId) : IRequest<ResultDto<string?>>;
}
