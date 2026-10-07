using AutoMapper;
using Business.Flows.DataService;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class CreateFlowAreaHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public CreateFlowAreaHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public Task<ResultDto<int>> HandleAsync(FlowAreaDto dto, CancellationToken ct)
        {
            FlowArea flowArea = _mapper.Map<FlowArea>(dto);
            return _dataService.FlowArea.CreateAsync(flowArea, ct);
        }
    }
}
