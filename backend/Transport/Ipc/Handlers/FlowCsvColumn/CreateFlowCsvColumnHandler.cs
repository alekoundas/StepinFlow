using AutoMapper;
using Business.Flows.DataService;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class CreateFlowCsvColumnHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public CreateFlowCsvColumnHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public Task<ResultDto<int>> HandleAsync(FlowCsvColumnDto dto, CancellationToken ct)
        {
            FlowCsvColumn input = _mapper.Map<FlowCsvColumn>(dto);
            return _dataService.FlowCsvColumn.CreateAsync(input, ct);
        }
    }
}
