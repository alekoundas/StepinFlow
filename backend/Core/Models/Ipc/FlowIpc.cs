using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetFlowQuery(int id) : IRequest<ResultDto<FlowDto>>;
    public record GetFlowTreeNodeQuery(int id) : IRequest<ResultDto<IEnumerable<TreeNodeDto>>>;
    public record GetLazyFlowQuery(LazyRequestDto dto) : IRequest<ResultDto<LazyResponseDto<FlowDto>>>;


    // ============== COMMANDS ==============
    public record CreateFlowCommand(FlowCreateDto dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowCommand(FlowDto dto) : IRequest<ResultDto<FlowDto>>;
    public record DeleteFlowCommand(int id) : IRequest<ResultDto<bool>>;

}
