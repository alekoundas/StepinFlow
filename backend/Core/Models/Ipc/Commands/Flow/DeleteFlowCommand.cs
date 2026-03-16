using MediatR;

namespace Core.Models.Ipc.Commands.Flow
{
    public record DeleteFlowCommand(int id) : IRequest<DeleteFlowCommandResponse>;
    public record DeleteFlowCommandResponse( bool success);
}
