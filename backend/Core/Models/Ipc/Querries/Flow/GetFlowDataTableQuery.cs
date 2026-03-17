using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.Flow
{
    public record GetFlowDataTableQuery(
     int Page,
     int Rows,
     string? SortField,
     int? SortOrder,
     Dictionary<string, object>? Filters
 ) : IRequest<GetFlowDataTableQueryResponse>;

    public record GetFlowDataTableQueryResponse(
        List<FlowDto> Data,
        int TotalRecords
    );
}
