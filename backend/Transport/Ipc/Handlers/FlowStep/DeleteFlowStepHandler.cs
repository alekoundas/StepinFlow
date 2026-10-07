using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class DeleteFlowStepHandler
    {
        private readonly DataService _dataService;

        public DeleteFlowStepHandler(DataService dataService)
        {
            _dataService = dataService;
        }

        public Task<ResultDto<bool>> HandleAsync(int id, CancellationToken ct)
        {
            return _dataService.FlowStep.DeleteAsync(id, ct);
        }
    }
}
