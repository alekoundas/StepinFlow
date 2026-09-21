using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetFlowStepTemplateQuery(int Id) : IRequest<ResultDto<FlowStepTemplateDto>>;
    public record GetLazyFlowStepTemplateQuery(LazyRequestDto Dto) : IRequest<ResultDto<LazyResponseDto<FlowStepTemplateDto>>>;


    // ============== COMMANDS ==============
    public record CreateFlowStepTemplateCommand(FlowStepTemplateDto Dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowStepTemplateCommand(FlowStepTemplateDto Dto) : IRequest<ResultDto<FlowStepTemplateDto>>;
    public record DeleteFlowStepTemplateCommand(int Id) : IRequest<ResultDto<bool>>;

}
