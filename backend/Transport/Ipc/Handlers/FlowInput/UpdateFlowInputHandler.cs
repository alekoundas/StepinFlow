using AutoMapper;
using Business.Flows.DataService;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class UpdateFlowInputHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public UpdateFlowInputHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public async Task<ResultDto<FlowInputDto>> HandleAsync(FlowInputDto dto, CancellationToken ct)
        {
            ResultDto<FlowInput> updated = await _dataService.FlowInput.UpdateAsync(dto, ct);
            if (!updated.IsSuccess)
                return ResultDto<FlowInputDto>.Failure(updated.ErrorMessage ?? string.Empty);

            FlowInputDto inputDto = _mapper.Map<FlowInputDto>(updated.Data);
            return ResultDto<FlowInputDto>.Success(inputDto);
        }
    }
}
