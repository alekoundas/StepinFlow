using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.Flow
{
    public record GetFlowTreeNodeQuery(int id) : IRequest<IEnumerable<TreeNodeDto>>;
}
