using Business.DataService.Services;
using Core.Models.Ipc.Commands.Flow;
using MediatR;

namespace Business.Ipc.Handlers.Flow
{
    public class CreateFlowHandler : IRequestHandler<CreateFlowCommand, CreateFlowCommandResponse>
    {
        private readonly IDataService _dataService;

        public CreateFlowHandler(IDataService dataService)
        {
            _dataService = dataService;
        }

        public Task<CreateFlowCommandResponse> Handle(CreateFlowCommand request, CancellationToken ct)
        {
            return Task.FromResult(new CreateFlowCommandResponse(1111, true));
        }
    }
}
