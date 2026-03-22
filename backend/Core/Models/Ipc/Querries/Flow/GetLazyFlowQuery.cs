using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.Flow
{
    public record GetLazyFlowQuery(LazyRequestDto dto) : IRequest<GetLazyFlowQueryResponse>;
    public record GetLazyFlowQueryResponse(LazyResponseDto dto);
}
