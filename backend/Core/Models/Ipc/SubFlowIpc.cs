using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetSubFlowQuery(int id) : IRequest<ResultDto<SubFlowDto>>;
    //public record GetSubFlowTreeNodeQuery(int id) : IRequest<IpcResponseDto<IEnumerable<TreeNodeDto>>>;
    public record GetLazySubFlowQuery(LazyRequestDto dto) : IRequest<ResultDto<LazyResponseDto<SubFlowDto>>>;


    // ============== COMMANDS ==============
    public record CreateSubFlowCommand(SubFlowDto dto) : IRequest<ResultDto<int>>;
    public record UpdateSubFlowCommand(SubFlowDto dto) : IRequest<ResultDto<SubFlowDto>>;
    public record DeleteSubFlowCommand(int id) : IRequest<ResultDto<bool>>;

}
