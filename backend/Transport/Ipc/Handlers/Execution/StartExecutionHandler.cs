using Business.Executions;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class StartExecutionHandler
    {
        private readonly IExecutionEngine _executionEngine;

        public StartExecutionHandler(IExecutionEngine executionEngine)
        {
            _executionEngine = executionEngine;
        }

        public async Task<ResultDto<int>> HandleAsync(ExecutionStartDto dto, CancellationToken ct)
        {
            try
            {
                // Not the request's token: the run outlives the call that asked for it.
                int executionId = await _executionEngine.StartAsync(dto, CancellationToken.None);
                return ResultDto<int>.Success(executionId);
            }
            catch (InvalidOperationException ex)
            {
                return ResultDto<int>.Failure(ex.Message);
            }
        }
    }
}
