using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.SubFlow
{
    public record GetSubFlowQuery(int id) : IRequest<GetSubFlowQueryResponse>;
    public record GetSubFlowQueryResponse(SubFlowDto entity);
}
