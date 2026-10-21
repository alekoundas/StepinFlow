using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class PasteFlowStepHandler
    {
        private readonly DataService _dataService;

        public PasteFlowStepHandler(DataService dataService)
        {
            _dataService = dataService;
        }

        public Task<ResultDto<int>> HandleAsync(FlowStepPasteDto dto, CancellationToken ct)
        {
            return _dataService.FlowStep.PasteAsync(dto, ct);
        }
    }
}
