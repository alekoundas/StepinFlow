using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
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

        public async Task<CreateFlowStepCommandResponse> Handle(CreateFlowStepCommand request, CancellationToken ct)
        {
            FlowStep? flowStep = _mapper.Map<FlowStep>(request.dto);
            if (flowStep == null)
                return new CreateFlowStepCommandResponse(-1, false);

            var count = await _dataService.AddAsync(flowStep);
            if (count <= 0)
                return new CreateFlowStepCommandResponse(-1, false);
            else
                return new CreateFlowStepCommandResponse(flowStep.Id, true);
        }
    }
}
