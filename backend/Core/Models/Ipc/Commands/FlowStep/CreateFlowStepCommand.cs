
using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.FlowStep
{
    public record CreateFlowStepCommand(FlowStepCreateDto dto) : IRequest<CreateFlowStepCommandResponse>;
    public record CreateFlowStepCommandResponse(int newId, bool success);
}
