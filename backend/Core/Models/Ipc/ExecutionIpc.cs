using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== COMMANDS ==============
    public record StartExecutionCommand(ExecutionStartDto dto) : IRequest<ResultDto<int>>;
    public record StopExecutionCommand() : IRequest<ResultDto<bool>>;
    public record PauseExecutionCommand() : IRequest<ResultDto<bool>>;
    public record ContinueExecutionCommand() : IRequest<ResultDto<bool>>;
    public record StepIntoExecutionCommand() : IRequest<ResultDto<bool>>;
    public record StepOverExecutionCommand() : IRequest<ResultDto<bool>>;
    public record SetExecutionBreakpointsCommand(List<int> flowStepIds) : IRequest<ResultDto<bool>>;


    // ============== QUERIES ==============
    public record GetExecutionQuery(int id) : IRequest<ResultDto<ExecutionDto>>;
    public record GetExecutionListQuery(int flowId) : IRequest<ResultDto<List<ExecutionDto>>>;
    public record GetExecutionStateQuery() : IRequest<ResultDto<ExecutionStateDto>>;
}
