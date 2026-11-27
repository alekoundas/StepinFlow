using Business.Executions;
using Business.Flows.DataService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Refused while an execution is using the flow, as its own or as a sub-flow it calls: the
    /// cleanup on Home would take the rows the execution is still writing against.
    /// </summary>
    public class DeleteFlowHandler
    {
        private readonly DataService _dataService;
        private readonly IExecutionEngine _executionEngine;

        public DeleteFlowHandler(DataService dataService, IExecutionEngine executionEngine)
        {
            _dataService = dataService;
            _executionEngine = executionEngine;
        }

        public Task<ResultDto<bool>> HandleAsync(int id, CancellationToken ct)
        {
            if (_executionEngine.IsExecuting(id))
                return Task.FromResult(ResultDto<bool>.Failure("An execution is using this flow. Stop it, then delete the flow."));

            return _dataService.Flow.DeleteAsync(id, ct);
        }
    }
}
