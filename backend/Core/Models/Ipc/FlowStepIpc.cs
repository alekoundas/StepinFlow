
using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{

    // ============== QUERIES ==============
    public record GetFlowStepQuery(int id) : IRequest<ResultDto<FlowStepDto>>;
    public record GetFlowStepTreeNodeQuery(int id) : IRequest<ResultDto<IEnumerable<TreeNodeDto>>>;
    public record GetLazyStepFlowQuery(LazyRequestDto dto) : IRequest<ResultDto<LazyResponseDto<FlowStepDto>>>;


    // ============== COMMANDS ==============
    public record CreateFlowStepCommand(FlowStepDto dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowStepCommand(FlowStepDto dto) : IRequest<ResultDto<FlowStepDto>>;
    public record DeleteFlowStepCommand(int id) : IRequest<ResultDto<bool>>;
}
