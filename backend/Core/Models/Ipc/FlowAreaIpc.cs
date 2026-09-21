using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetFlowAreaQuery(int Id) : IRequest<ResultDto<FlowAreaDto>>;
    public record GetLazyFlowAreaQuery(LazyRequestDto Dto   ) : IRequest<ResultDto<LazyResponseDto<FlowAreaDto>>>;
    public record GetFlowAreaPreviewQuery(int Id) : IRequest<ResultDto<FlowAreaPreviewDto>>;


    // ============== COMMANDS ==============
    public record CreateFlowAreaCommand(FlowAreaDto Dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowAreaCommand(FlowAreaDto Dto) : IRequest<ResultDto<FlowAreaDto>>;
    public record DeleteFlowAreaCommand(int Id) : IRequest<ResultDto<bool>>;
}
