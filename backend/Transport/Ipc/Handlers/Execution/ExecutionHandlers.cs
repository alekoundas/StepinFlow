using Business.Services.ExecutionService;
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

    public class StopExecutionHandler : ExecutionCommandHandler
    {
        public StopExecutionHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            return Apply(engine => engine.Stop());
        }
    }

    public class PauseExecutionHandler : ExecutionCommandHandler
    {
        public PauseExecutionHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            return Apply(engine => engine.Pause());
        }
    }

    public class ContinueExecutionHandler : ExecutionCommandHandler
    {
        public ContinueExecutionHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            return Apply(engine => engine.Continue());
        }
    }

    public class StepIntoExecutionHandler : ExecutionCommandHandler
    {
        public StepIntoExecutionHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            return Apply(engine => engine.StepInto());
        }
    }

    public class StepOverExecutionHandler : ExecutionCommandHandler
    {
        public StepOverExecutionHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            return Apply(engine => engine.StepOver());
        }
    }

    public class SetExecutionBreakpointsHandler : ExecutionCommandHandler
    {
        public SetExecutionBreakpointsHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> HandleAsync(List<int> flowStepIds, CancellationToken ct)
        {
            return Apply(engine => engine.SetBreakpoints(flowStepIds));
        }
    }
}
