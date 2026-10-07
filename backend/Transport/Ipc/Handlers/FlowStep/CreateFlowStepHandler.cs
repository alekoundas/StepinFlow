using AutoMapper;
using Business.Flows.DataService;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class CreateFlowStepHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public CreateFlowStepHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public Task<ResultDto<int>> HandleAsync(FlowStepDto dto, CancellationToken ct)
        {
            FlowStep flowStep = _mapper.Map<FlowStep>(dto);
            return _dataService.FlowStep.CreateAsync(flowStep, dto.FlowStepTemplates, ct);
        }
    }
}
