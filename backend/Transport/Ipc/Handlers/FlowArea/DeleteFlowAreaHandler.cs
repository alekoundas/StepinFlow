using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class DeleteFlowAreaHandler
    {
        private readonly DataService _dataService;

        public DeleteFlowAreaHandler(DataService dataService)
        {
            _dataService = dataService;
        }

        public Task<ResultDto<bool>> HandleAsync(int id, CancellationToken ct)
        {
            return _dataService.FlowArea.DeleteAsync(id, ct);
        }
    }
}
