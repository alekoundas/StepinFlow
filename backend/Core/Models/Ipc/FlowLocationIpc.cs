using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetFlowLocationQuery(int id) : IRequest<ResultDto<FlowLocationDto>>;


    // ============== COMMANDS ==============
    public record CreateFlowLocationCommand(FlowLocationDto dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowLocationCommand(FlowLocationDto dto) : IRequest<ResultDto<FlowLocationDto>>;
    public record DeleteFlowLocationCommand(int id) : IRequest<ResultDto<bool>>;
}
