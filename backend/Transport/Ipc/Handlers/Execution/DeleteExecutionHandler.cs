using Business.Executions;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class DeleteExecutionHandler
    {
        private readonly IExecutionDeleteService _executionDeleteService;

        public DeleteExecutionHandler(IExecutionDeleteService executionDeleteService)
        {
            _executionDeleteService = executionDeleteService;
        }

        public Task<ResultDto<bool>> HandleAsync(int executionId, CancellationToken ct)
        {
            return _executionDeleteService.DeleteAsync(executionId, ct);
        }
    }
}
