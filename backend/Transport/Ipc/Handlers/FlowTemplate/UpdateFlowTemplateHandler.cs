using AutoMapper;
using Business.Flows.DataService;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class UpdateFlowTemplateHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public UpdateFlowTemplateHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public async Task<ResultDto<FlowTemplateDto>> HandleAsync(FlowTemplateDto dto, CancellationToken ct)
        {
            ResultDto<FlowTemplate> updated = await _dataService.FlowTemplate.UpdateAsync(dto, ct);
            if (!updated.IsSuccess)
                return ResultDto<FlowTemplateDto>.Failure(updated.ErrorMessage ?? string.Empty);

            FlowTemplateDto flowTemplateDto = _mapper.Map<FlowTemplateDto>(updated.Data);

            return ResultDto<FlowTemplateDto>.Success(flowTemplateDto);
        }
    }
}
