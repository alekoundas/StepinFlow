using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.Flow
{
    public record UpdateFlowCommand(FlowDto dto) : IRequest<UpdateFlowCommandResponse>;
    public record UpdateFlowCommandResponse(FlowDto? dto, bool success);
}
