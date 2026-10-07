using AutoMapper;
using Business.Flows.DataService;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class UpdateFlowStepHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public UpdateFlowStepHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public async Task<ResultDto<FlowStepDto>> HandleAsync(FlowStepDto dto, CancellationToken ct)
        {
            ResultDto<FlowStep> updated = await _dataService.FlowStep.UpdateAsync(dto, ct);
            if (!updated.IsSuccess)
                return ResultDto<FlowStepDto>.Failure(updated.ErrorMessage ?? string.Empty);

            FlowStepDto flowStepDto = _mapper.Map<FlowStepDto>(updated.Data);

            return ResultDto<FlowStepDto>.Success(flowStepDto);
        }
    }
}
