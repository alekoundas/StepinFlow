using Business.Executions;
using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>Every execution of one flow, but one still going. Answers how many went.</summary>
    public class DeleteFlowExecutionsHandler
    {
        private readonly DataService _dataService;
        private readonly IExecutionEngine _executionEngine;

        public DeleteFlowExecutionsHandler(DataService dataService, IExecutionEngine executionEngine)
        {
            _dataService = dataService;
            _executionEngine = executionEngine;
        }

        public Task<ResultDto<int>> HandleAsync(int flowId, CancellationToken ct)
        {
            int exceptExecutionId = 0;
            if (_executionEngine.IsRunning)
                exceptExecutionId = _executionEngine.ExecutionId;

            return _dataService.Execution.DeleteForFlowAsync(flowId, exceptExecutionId, ct);
        }
    }
}
