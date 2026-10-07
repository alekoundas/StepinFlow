using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class MoveFlowStepHandler
    {
        private readonly DataService _dataService;

        public MoveFlowStepHandler(DataService dataService)
        {
            _dataService = dataService;
        }

        public Task<ResultDto<bool>> HandleAsync(FlowStepMoveDto dto, CancellationToken ct)
        {
            return _dataService.FlowStep.MoveAsync(dto, ct);
        }
    }
}
