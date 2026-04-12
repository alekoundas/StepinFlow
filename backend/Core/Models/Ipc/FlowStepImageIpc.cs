using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetFlowStepImageQuery(int id) : IRequest<ResultDto<FlowStepImageDto>>;
    public record GetLazyFlowStepImageQuery(LazyRequestDto dto) : IRequest<ResultDto<LazyResponseDto<FlowStepImageDto>>>;


    // ============== COMMANDS ==============
    public record CreateFlowStepImageCommand(FlowStepImageDto dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowStepImageCommand(FlowStepImageDto dto) : IRequest<ResultDto<FlowStepImageDto>>;
    public record DeleteFlowStepImageCommand(int id) : IRequest<ResultDto<bool>>;

}
