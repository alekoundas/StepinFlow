using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetFlowStepTemplateQuery(int id) : IRequest<ResultDto<FlowStepTemplateDto>>;
    public record GetLazyFlowStepTemplateQuery(LazyRequestDto dto) : IRequest<ResultDto<LazyResponseDto<FlowStepTemplateDto>>>;


    // ============== COMMANDS ==============
    public record CreateFlowStepTemplateCommand(FlowStepTemplateDto dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowStepTemplateCommand(FlowStepTemplateDto dto) : IRequest<ResultDto<FlowStepTemplateDto>>;
    public record DeleteFlowStepTemplateCommand(int id) : IRequest<ResultDto<bool>>;

}
