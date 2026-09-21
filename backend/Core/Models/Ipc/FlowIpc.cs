using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetFlowQuery(int Id) : IRequest<ResultDto<FlowDto>>;
    public record ValidateFlowQuery(int Id) : IRequest<ResultDto<FlowValidationResultDto>>;
    public record GetFlowTreeNodeQuery(int Id) : IRequest<ResultDto<IEnumerable<TreeNodeDto>>>;
    public record GetLazyFlowQuery(LazyRequestDto Dto) : IRequest<ResultDto<LazyResponseDto<FlowDto>>>;

    /// <summary>Error and warning counts for a page of flows, fetched after the list renders.</summary>
    public record GetFlowHealthQuery(FlowHealthRequestDto Dto) : IRequest<ResultDto<IReadOnlyList<FlowHealthDto>>>;

    /// <summary>The flows that invoke this one, so editing a shared sub-flow is a decision.</summary>
    public record GetFlowCallersQuery(int Id) : IRequest<ResultDto<IReadOnlyList<LookupItemDto>>>;


    // ============== COMMANDS ==============
    public record CreateFlowCommand(FlowDto Dto) : IRequest<ResultDto<int>>;
    public record UpdateFlowCommand(FlowDto Dto) : IRequest<ResultDto<FlowDto>>;
    public record DeleteFlowCommand(int Id) : IRequest<ResultDto<bool>>;

    /// <summary>One way: a flow becomes callable and never stops being callable.</summary>
    public record PromoteFlowToSubFlowCommand(int Id) : IRequest<ResultDto<bool>>;

    public record ExtractSubFlowCommand(ExtractSubFlowDto Dto) : IRequest<ResultDto<ExtractSubFlowResultDto>>;

    /// <summary>Writes the flow out as a script with its template images beside it.</summary>
    public record ExportFlowCommand(FlowExportRequestDto Dto) : IRequest<ResultDto<FlowExportResultDto>>;

}
