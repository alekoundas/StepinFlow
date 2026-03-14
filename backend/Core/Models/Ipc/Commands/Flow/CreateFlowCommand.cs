using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.Flow
{
    public record CreateFlowCommand(FlowCreateDto dto) : IRequest<CreateFlowCommandResponse>;
    public record CreateFlowCommandResponse(int newId, bool success);
}
