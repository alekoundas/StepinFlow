using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.Flow
{
    public record GetFlowDataTableQuery(DataTableRequestDto dto) : IRequest<GetFlowDataTableQueryResponse>;
    public record GetFlowDataTableQueryResponse(DataTableResponseDto dto);
}
