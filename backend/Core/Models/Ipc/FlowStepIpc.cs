
using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{

    // ============== QUERIES ==============
    public record GetFlowStepQuery(int id) : IRequest<ResultDto<FlowStepDto>>;
    public record GetFlowStepTreeNodeQuery(TreeNodeRequestDto dto) : IRequest<ResultDto<IEnumerable<TreeNodeDto>>>;
    public record GetFlowStepTreeNodesRecursiveQuery(int flowId) : IRequest<ResultDto<IEnumerable<TreeNodeDto>>>;
    public record GetLazyStepFlowQuery(LazyRequestDto dto) : IRequest<ResultDto<LazyResponseDto<FlowStepDto>>>;
    public record GetFlowStepMovePreviewQuery(FlowStepMoveDto dto) : IRequest<ResultDto<FlowStepMovePreviewDto>>;
    public record TestImageSearchQuery(FlowStepDto dto) : IRequest<ResultDto<ImageSearchTestResultDto>>;
    public record TestRunCommandQuery(FlowStepDto dto) : IRequest<ResultDto<RunCommandTestResultDto>>;
    public record TestReadTextQuery(FlowStepDto dto) : IRequest<ResultDto<ReadTextTestResultDto>>;


    // ============== COMMANDS ==============
    public record CreateFlowStepCommand(FlowStepDto dto) : IRequest<ResultDto<int>>;
    public record CreateFlowStepsCommand(FlowDraftDto dto) : IRequest<ResultDto<FlowDraftResultDto>>;
    public record UpdateFlowStepCommand(FlowStepDto dto) : IRequest<ResultDto<FlowStepDto>>;
    public record DeleteFlowStepCommand(int id) : IRequest<ResultDto<bool>>;
    public record MoveFlowStepCommand(FlowStepMoveDto dto) : IRequest<ResultDto<bool>>;
}
