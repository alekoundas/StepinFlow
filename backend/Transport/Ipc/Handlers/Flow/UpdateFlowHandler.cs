using AutoMapper;
using Business.Flows.DataService;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class UpdateFlowHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public UpdateFlowHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public async Task<ResultDto<FlowDto>> HandleAsync(FlowDto dto, CancellationToken ct)
        {
            ResultDto<Flow> updated = await _dataService.Flow.UpdateAsync(dto, ct);
            if (!updated.IsSuccess)
                return ResultDto<FlowDto>.Failure(updated.ErrorMessage ?? string.Empty);

            FlowDto updatedDto = _mapper.Map<FlowDto>(updated.Data);
            return ResultDto<FlowDto>.Success(updatedDto);
        }
    }
}
