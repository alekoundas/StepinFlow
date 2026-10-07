using AutoMapper;
using Business.Flows.DataService;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class CreateFlowHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public CreateFlowHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public Task<ResultDto<int>> HandleAsync(FlowDto dto, CancellationToken ct)
        {
            string? invalidName = FileNameHelper.Validate(dto.Name);
            if (invalidName != null)
                return Task.FromResult(ResultDto<int>.Failure(invalidName));

            Flow flow = _mapper.Map<Flow>(dto);

            return _dataService.Flow.CreateAsync(flow, ct);
        }
    }
}
