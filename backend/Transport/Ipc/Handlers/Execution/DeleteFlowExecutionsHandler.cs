using Business.Executions;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>Every execution of one flow, but one still going. Answers how many went.</summary>
    public class DeleteFlowExecutionsHandler
    {
        private readonly IExecutionDeleteService _executionDeleteService;

        public DeleteFlowExecutionsHandler(IExecutionDeleteService executionDeleteService)
        {
            _executionDeleteService = executionDeleteService;
        }

        public Task<ResultDto<int>> HandleAsync(int flowId, CancellationToken ct)
        {
            return _executionDeleteService.DeleteForFlowAsync(flowId, ct);
        }
    }
}
