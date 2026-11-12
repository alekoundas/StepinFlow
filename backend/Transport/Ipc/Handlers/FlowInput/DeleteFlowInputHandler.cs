using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class DeleteFlowInputHandler
    {
        private readonly DataService _dataService;

        public DeleteFlowInputHandler(DataService dataService)
        {
            _dataService = dataService;
        }

        public Task<ResultDto<bool>> HandleAsync(int id, CancellationToken ct)
        {
            return _dataService.FlowInput.DeleteAsync(id, ct);
        }
    }
}
