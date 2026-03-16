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
 ) : IRequest<GetFlowDataTableResponse>;

    public record GetFlowDataTableResponse(
        List<FlowDto> Data,
        int TotalRecords
    );
}
