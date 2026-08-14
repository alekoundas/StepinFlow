using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetFlowPointQuery(int id) : IRequest<ResultDto<FlowPointDto>>;
    public record GetFlowPointPreviewQuery(int id) : IRequest<ResultDto<ScreenPointDto>>;


    // ============== COMMANDS ==============
    public record CreateFlowPointCommand(FlowPointDto dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowPointCommand(FlowPointDto dto) : IRequest<ResultDto<FlowPointDto>>;
    public record DeleteFlowPointCommand(int id) : IRequest<ResultDto<bool>>;
}
