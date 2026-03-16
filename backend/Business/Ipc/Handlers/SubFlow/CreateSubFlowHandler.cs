using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc.Commands.Flow;
using Core.Models.Ipc.Commands.SubFlow;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class CreateSubFlowHandler : IRequestHandler<CreateSubFlowCommand, CreateSubFlowCommandResponse>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public CreateSubFlowHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public Task<CreateSubFlowCommandResponse> Handle(CreateSubFlowCommand request, CancellationToken ct)
        {
            return Task.FromResult(new CreateSubFlowCommandResponse(1111, true));
        }
    }
}
