using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.FlowSearchArea
{
    public record CreateFlowSearchAreaCommand(FlowSearchAreaCreateDto dto) : IRequest<CreateFlowSearchAreaCommandResponse>;
    public record CreateFlowSearchAreaCommandResponse(int newId, bool success);
}
