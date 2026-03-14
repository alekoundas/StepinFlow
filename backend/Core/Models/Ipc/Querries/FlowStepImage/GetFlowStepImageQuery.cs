using Core.Models.Database;
using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.FlowStepImage
{
    public record GetFlowStepImageQuery(int id) : IRequest<GetFlowStepImageQueryResponse>;
    public record GetFlowStepImageQueryResponse(FlowStepImageDto entity);
}
