using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class DeleteFlowHandler
    {
        private readonly DataService _dataService;

        public DeleteFlowHandler(DataService dataService)
        {
            _dataService = dataService;
        }

        public Task<ResultDto<bool>> HandleAsync(int id, CancellationToken ct)
        {
            return _dataService.Flow.DeleteAsync(id, ct);
        }
    }
}
