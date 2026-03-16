using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Ipc.Commands.Flow;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class DeleteFlowHandler : IRequestHandler<DeleteFlowCommand, DeleteFlowCommandResponse>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public DeleteFlowHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public async Task<DeleteFlowCommandResponse> Handle(DeleteFlowCommand request, CancellationToken ct)
        {
            var count = await _dataService.DeleteByIdAsync<Flow>(request.id);
            if (count <= 0)
                return new DeleteFlowCommandResponse(false);
            else
                return new DeleteFlowCommandResponse(true);
        }
    }
}
