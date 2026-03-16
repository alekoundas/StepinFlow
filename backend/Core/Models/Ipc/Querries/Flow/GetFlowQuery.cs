using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.Flow
{
    public record GetFlowQuery(int id) : IRequest<GetFlowQueryResponse>;
    public record GetFlowQueryResponse(FlowDto? entity, bool success);
}
