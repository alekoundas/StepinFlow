using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.FlowStep
{
    public record GetFlowStepQuery(int id) : IRequest<GetFlowStepQueryResponse>;
    public record GetFlowStepQueryResponse(FlowStepDto entity);
}
