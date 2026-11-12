using AutoMapper;
using Business.Flows.DataService;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class CreateFlowInputHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public CreateFlowInputHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public Task<ResultDto<int>> HandleAsync(FlowInputDto dto, CancellationToken ct)
        {
            FlowInput input = _mapper.Map<FlowInput>(dto);
            return _dataService.FlowInput.CreateAsync(input, ct);
        }
    }
}
