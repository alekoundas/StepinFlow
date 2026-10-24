using AutoMapper;
using Business.FlowScript;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;

namespace Transport.Ipc.Handlers
{
    /// <summary>The line a step being edited will print as, comment included.</summary>
    public class GetFlowStepScriptLineHandler
    {
        private readonly IMapper _mapper;
        private readonly IFlowStepScriptLineService _scriptLineService;

        public GetFlowStepScriptLineHandler(IMapper mapper, IFlowStepScriptLineService scriptLineService)
        {
            _mapper = mapper;
            _scriptLineService = scriptLineService;
        }

        public async Task<ResultDto<FlowStepScriptLineDto>> HandleAsync(FlowStepDto dto, CancellationToken ct)
        {
            FlowStep step = _mapper.Map<FlowStep>(dto);
            List<FlowStepTemplateDto> pictured = dto.FlowStepTemplates.Where(x => x.FlowTemplate != null).ToList();
            step.FlowStepTemplates = _mapper.Map<List<FlowStepTemplate>>(pictured);

            try
            {
                FlowStepScriptLineDto line = await _scriptLineService.WriteAsync(step, ct);

                return ResultDto<FlowStepScriptLineDto>.Success(line);
            }
            catch (InvalidOperationException ex)
            {
                // A half-filled form can be a step no line can be written for yet.
                return ResultDto<FlowStepScriptLineDto>.Failure(ex.Message);
            }
        }
    }
}
