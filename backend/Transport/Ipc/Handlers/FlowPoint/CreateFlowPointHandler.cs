using AutoMapper;
using Business.Flows.DataService;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class CreateFlowPointHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public CreateFlowPointHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public Task<ResultDto<int>> HandleAsync(FlowPointDto dto, CancellationToken ct)
        {
            FlowPoint flowPoint = _mapper.Map<FlowPoint>(dto);
            return _dataService.FlowPoint.CreateAsync(flowPoint, ct);
        }
    }
}
