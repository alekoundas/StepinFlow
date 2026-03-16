using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc.Commands.Flow;
using Core.Models.Ipc.Commands.FlowStep;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class CreateFlowStepHandler : IRequestHandler<CreateFlowStepCommand, CreateFlowStepCommandResponse>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public CreateFlowStepHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public Task<CreateFlowStepCommandResponse> Handle(CreateFlowStepCommand request, CancellationToken ct)
        {
            return Task.FromResult(new CreateFlowStepCommandResponse(1111, true));
        }
    }
}
