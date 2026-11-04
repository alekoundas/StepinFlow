using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SetFlowStepDisabledHandler
    {
        private readonly DataService _dataService;

        public SetFlowStepDisabledHandler(DataService dataService)
        {
            _dataService = dataService;
        }

        public Task<ResultDto<bool>> HandleAsync(FlowStepDisabledDto dto, CancellationToken ct)
        {
            return _dataService.FlowStep.SetDisabledAsync(dto, ct);
        }
    }
}
