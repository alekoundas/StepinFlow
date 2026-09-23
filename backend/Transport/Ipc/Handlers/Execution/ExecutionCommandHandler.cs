using Business.Services.ExecutionService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public abstract class ExecutionCommandHandler
    {
        private readonly IExecutionEngine _executionEngine;

        protected ExecutionCommandHandler(IExecutionEngine executionEngine)
        {
            _executionEngine = executionEngine;
        }

        protected Task<ResultDto<bool>> Apply(Action<IExecutionEngine> command)
        {
            if (!_executionEngine.IsRunning)
                return Task.FromResult(ResultDto<bool>.Failure("Nothing is running."));

            command(_executionEngine);
            return Task.FromResult(ResultDto<bool>.Success(true));
        }
    }
}
