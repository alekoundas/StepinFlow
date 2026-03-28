using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetFlowSearchAreaQuery(int id) : IRequest<ResultDto<FlowSearchAreaDto>>;
    public record GetLazyFlowSearchAreaQuery(LazyRequestDto dto) : IRequest<ResultDto<LazyResponseDto<FlowSearchAreaDto>>>;


    // ============== COMMANDS ==============
    public record CreateFlowSearchAreaCommand(FlowSearchAreaCreateDto dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowSearchAreaCommand(FlowDto dto) : IRequest<ResultDto<FlowSearchAreaDto>>;
    public record DeleteFlowSearchAreaCommand(int id) : IRequest<ResultDto<bool>>;
}
