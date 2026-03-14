using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.SubFlow
{
    public record CreateSubFlowCommand(SubFlowCreateDto dto) : IRequest<CreateSubFlowCommandResponse>;
    public record CreateSubFlowCommandResponse(int newId, bool success);
}
