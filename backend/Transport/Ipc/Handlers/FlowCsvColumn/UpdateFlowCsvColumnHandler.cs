using AutoMapper;
using Business.Flows.DataService;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class UpdateFlowCsvColumnHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public UpdateFlowCsvColumnHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public async Task<ResultDto<FlowCsvColumnDto>> HandleAsync(FlowCsvColumnDto dto, CancellationToken ct)
        {
            ResultDto<FlowCsvColumn> updated = await _dataService.FlowCsvColumn.UpdateAsync(dto, ct);
            if (!updated.IsSuccess)
                return ResultDto<FlowCsvColumnDto>.Failure(updated.ErrorMessage ?? string.Empty);

            FlowCsvColumnDto inputDto = _mapper.Map<FlowCsvColumnDto>(updated.Data);
            return ResultDto<FlowCsvColumnDto>.Success(inputDto);
        }
    }
}
