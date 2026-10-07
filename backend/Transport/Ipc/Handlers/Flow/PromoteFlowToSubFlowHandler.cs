using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Converts a flow to SubFlow.
    /// </summary>
    public class PromoteFlowToSubFlowHandler
    {
        private readonly DataService _dataService;

        public PromoteFlowToSubFlowHandler(DataService dataService)
        {
            _dataService = dataService;
        }

        public Task<ResultDto<bool>> HandleAsync(int id, CancellationToken ct)
        {
            return _dataService.Flow.PromoteToSubFlowAsync(id, ct);
        }
    }
}
