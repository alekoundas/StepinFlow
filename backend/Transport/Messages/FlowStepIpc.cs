
using Core.Models.Dtos;
using MediatR;

namespace Transport.Messages
{

    // ============== QUERIES ==============
    public record GetFlowStepQuery(int Id) : IRequest<ResultDto<FlowStepDto>>;
    public record GetFlowStepTreeNodeQuery(TreeNodeRequestDto Dto) : IRequest<ResultDto<IEnumerable<TreeNodeDto>>>;
    public record GetFlowStepTreeNodesRecursiveQuery(int FlowId) : IRequest<ResultDto<IEnumerable<TreeNodeDto>>>;
    public record GetLazyStepFlowQuery(LazyRequestDto Dto) : IRequest<ResultDto<LazyResponseDto<FlowStepDto>>>;
    public record GetFlowStepMovePreviewQuery(FlowStepMoveDto Dto) : IRequest<ResultDto<FlowStepMovePreviewDto>>;
    public record GetFlowStepDeleteImpactQuery(int Id) : IRequest<ResultDto<FlowStepDeleteImpactDto>>;
    public record TestImageSearchQuery(FlowStepDto Dto) : IRequest<ResultDto<ImageSearchTestResultDto>>;
    public record TestRunCommandQuery(FlowStepDto Dto) : IRequest<ResultDto<RunCommandTestResultDto>>;
    public record TestSearchTextQuery(FlowStepDto Dto) : IRequest<ResultDto<SearchTextTestResultDto>>;


    // ============== COMMANDS ==============
    public record CreateFlowStepCommand(FlowStepDto Dto) : IRequest<ResultDto<int>>;
    public record CreateFlowStepsCommand(FlowDraftDto Dto) : IRequest<ResultDto<FlowDraftResultDto>>;
    public record UpdateFlowStepCommand(FlowStepDto Dto) : IRequest<ResultDto<FlowStepDto>>;
    public record DeleteFlowStepCommand(int Id) : IRequest<ResultDto<bool>>;
    public record MoveFlowStepCommand(FlowStepMoveDto Dto) : IRequest<ResultDto<bool>>;
}
