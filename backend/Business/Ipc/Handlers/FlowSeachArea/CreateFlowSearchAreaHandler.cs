using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc.Commands.Flow;
using Core.Models.Ipc.Commands.FlowSearchArea;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class CreateFlowSearchAreaHandler : IRequestHandler<CreateFlowSearchAreaCommand, CreateFlowSearchAreaCommandResponse>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public CreateFlowSearchAreaHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public Task<CreateFlowSearchAreaCommandResponse> Handle(CreateFlowSearchAreaCommand request, CancellationToken ct)
        {
            return Task.FromResult(new CreateFlowSearchAreaCommandResponse(1111, true));
        }
    }
}
