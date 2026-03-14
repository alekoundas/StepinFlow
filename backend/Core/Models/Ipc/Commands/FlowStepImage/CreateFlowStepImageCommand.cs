
using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.FlowStepImage
{
    public record CreateFlowStepImageCommand(FlowStepImageCreateDto dto) : IRequest<CreateFlowStepImageCommandResponse>;
    public record CreateFlowStepImageCommandResponse(int newId, bool success);
}
