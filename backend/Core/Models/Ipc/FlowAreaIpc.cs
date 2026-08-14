using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetFlowAreaQuery(int id) : IRequest<ResultDto<FlowAreaDto>>;
    public record GetLazyFlowAreaQuery(LazyRequestDto dto) : IRequest<ResultDto<LazyResponseDto<FlowAreaDto>>>;
    public record GetFlowAreaPreviewQuery(int id) : IRequest<ResultDto<FlowAreaPreviewDto>>;


    // ============== COMMANDS ==============
    public record CreateFlowAreaCommand(FlowAreaDto dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowAreaCommand(FlowAreaDto dto) : IRequest<ResultDto<FlowAreaDto>>;
    public record DeleteFlowAreaCommand(int id) : IRequest<ResultDto<bool>>;
}
