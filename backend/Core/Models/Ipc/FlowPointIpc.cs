using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetFlowPointQuery(int Id) : IRequest<ResultDto<FlowPointDto>>;
    public record GetFlowPointPreviewQuery(int Id) : IRequest<ResultDto<ScreenPointDto>>;


    // ============== COMMANDS ==============
    public record CreateFlowPointCommand(FlowPointDto Dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowPointCommand(FlowPointDto Dto) : IRequest<ResultDto<FlowPointDto>>;
    public record DeleteFlowPointCommand(int Id) : IRequest<ResultDto<bool>>;
}
