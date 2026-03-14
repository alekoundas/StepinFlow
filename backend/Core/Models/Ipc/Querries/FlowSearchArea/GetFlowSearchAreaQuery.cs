using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc.Commands.FlowSearchArea
{
    public record GetFlowSearchAreaQuery(int id) : IRequest<GetFlowSeachAreaQueryResponse>;
    public record GetFlowSeachAreaQueryResponse(FlowSearchAreaDto entity);
}
