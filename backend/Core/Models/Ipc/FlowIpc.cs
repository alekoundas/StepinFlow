using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetFlowQuery(int id) : IRequest<ResultDto<FlowDto>>;
    public record ValidateFlowQuery(int id) : IRequest<ResultDto<FlowValidationResultDto>>;
    public record GetFlowTreeNodeQuery(int id) : IRequest<ResultDto<IEnumerable<TreeNodeDto>>>;
    public record GetLazyFlowQuery(LazyRequestDto dto) : IRequest<ResultDto<LazyResponseDto<FlowDto>>>;

    /// <summary>The flows that invoke this one, so editing a shared sub-flow is a decision.</summary>
    public record GetFlowCallersQuery(int id) : IRequest<ResultDto<IReadOnlyList<LookupItemDto>>>;


    // ============== COMMANDS ==============
    public record CreateFlowCommand(FlowDto dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowCommand(FlowDto dto) : IRequest<ResultDto<FlowDto>>;
    public record DeleteFlowCommand(int id) : IRequest<ResultDto<bool>>;

    /// <summary>One way: a flow becomes callable and never stops being callable.</summary>
    public record PromoteFlowToSubFlowCommand(int id) : IRequest<ResultDto<bool>>;

    public record ExtractSubFlowCommand(ExtractSubFlowDto dto) : IRequest<ResultDto<ExtractSubFlowResultDto>>;

}
