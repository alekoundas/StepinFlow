using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class DeleteFlowPointHandler
    {
        private readonly DataService _dataService;

        public DeleteFlowPointHandler(DataService dataService)
        {
            _dataService = dataService;
        }

        public Task<ResultDto<bool>> HandleAsync(int id, CancellationToken ct)
        {
            return _dataService.FlowPoint.DeleteAsync(id, ct);
        }
    }
}
