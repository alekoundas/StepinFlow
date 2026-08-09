using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetFlowSearchAreaQuery(int id) : IRequest<ResultDto<FlowSearchAreaDto>>;
    public record GetLazyFlowSearchAreaQuery(LazyRequestDto dto) : IRequest<ResultDto<LazyResponseDto<FlowSearchAreaDto>>>;
    public record GetFlowSearchAreaPreviewQuery(int id) : IRequest<ResultDto<FlowSearchAreaPreviewDto>>;


    // ============== COMMANDS ==============
    public record CreateFlowSearchAreaCommand(FlowSearchAreaDto dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowSearchAreaCommand(FlowSearchAreaDto dto) : IRequest<ResultDto<FlowSearchAreaDto>>;
    public record DeleteFlowSearchAreaCommand(int id) : IRequest<ResultDto<bool>>;
}
