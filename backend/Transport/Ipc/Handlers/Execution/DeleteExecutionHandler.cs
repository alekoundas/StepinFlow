using Business.Executions;
using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>The one executing now is never deleted: the cleanup on Home would take the rows it is writing.</summary>
    public class DeleteExecutionHandler
    {
        private readonly DataService _dataService;
        private readonly IExecutionEngine _executionEngine;

        public DeleteExecutionHandler(DataService dataService, IExecutionEngine executionEngine)
        {
            _dataService = dataService;
            _executionEngine = executionEngine;
        }

        public Task<ResultDto<bool>> HandleAsync(int executionId, CancellationToken ct)
        {
            if (_executionEngine.IsRunning && _executionEngine.ExecutionId == executionId)
                return Task.FromResult(ResultDto<bool>.Failure("This execution is still going. Stop it, then delete it."));

            return _dataService.Execution.DeleteAsync(executionId, ct);
        }
    }
}
