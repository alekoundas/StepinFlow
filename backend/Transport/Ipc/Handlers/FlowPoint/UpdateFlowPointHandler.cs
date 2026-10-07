using AutoMapper;
using Business.Flows.DataService;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class UpdateFlowPointHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public UpdateFlowPointHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public async Task<ResultDto<FlowPointDto>> HandleAsync(FlowPointDto dto, CancellationToken ct)
        {
            ResultDto<FlowPoint> updated = await _dataService.FlowPoint.UpdateAsync(dto, ct);
            if (!updated.IsSuccess)
                return ResultDto<FlowPointDto>.Failure(updated.ErrorMessage ?? string.Empty);

            FlowPointDto flowPointDto = _mapper.Map<FlowPointDto>(updated.Data);

            return ResultDto<FlowPointDto>.Success(_mapper.Map<FlowPointDto>(flowPointDto));
        }
    }
}
