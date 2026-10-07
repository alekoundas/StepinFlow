using AutoMapper;
using Business.Flows.DataService;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class UpdateFlowAreaHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public UpdateFlowAreaHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public async Task<ResultDto<FlowAreaDto>> HandleAsync(FlowAreaDto dto, CancellationToken ct)
        {
            ResultDto<FlowArea> updated = await _dataService.FlowArea.UpdateAsync(dto, ct);
            if (!updated.IsSuccess)
                return ResultDto<FlowAreaDto>.Failure(updated.ErrorMessage ?? string.Empty);

            FlowAreaDto flowAreaDto = _mapper.Map<FlowAreaDto>(updated.Data);

            return ResultDto<FlowAreaDto>.Success(flowAreaDto);
        }
    }
}
