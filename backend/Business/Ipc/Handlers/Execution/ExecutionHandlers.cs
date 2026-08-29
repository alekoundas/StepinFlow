using Business.Services.ExecutionService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class StartExecutionHandler : IRequestHandler<StartExecutionCommand, ResultDto<int>>
    {
        private readonly IExecutionEngine _executionEngine;

        public StartExecutionHandler(IExecutionEngine executionEngine)
        {
            _executionEngine = executionEngine;
        }

        public async Task<ResultDto<int>> Handle(StartExecutionCommand request, CancellationToken _)
        {
            try
            {
                // Not the request's token: the run outlives the call that asked for it.
                int executionId = await _executionEngine.StartAsync(request.dto, CancellationToken.None);
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
        protected readonly IExecutionEngine _executionEngine;

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

    public class StopExecutionHandler : ExecutionCommandHandler, IRequestHandler<StopExecutionCommand, ResultDto<bool>>
    {
        public StopExecutionHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> Handle(StopExecutionCommand request, CancellationToken ct)
        {
            return Apply(engine => engine.Stop());
        }
    }

    public class PauseExecutionHandler : ExecutionCommandHandler, IRequestHandler<PauseExecutionCommand, ResultDto<bool>>
    {
        public PauseExecutionHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> Handle(PauseExecutionCommand request, CancellationToken ct)
        {
            return Apply(engine => engine.Pause());
        }
    }

    public class ContinueExecutionHandler : ExecutionCommandHandler, IRequestHandler<ContinueExecutionCommand, ResultDto<bool>>
    {
        public ContinueExecutionHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> Handle(ContinueExecutionCommand request, CancellationToken ct)
        {
            return Apply(engine => engine.Continue());
        }
    }

    public class StepIntoExecutionHandler : ExecutionCommandHandler, IRequestHandler<StepIntoExecutionCommand, ResultDto<bool>>
    {
        public StepIntoExecutionHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> Handle(StepIntoExecutionCommand request, CancellationToken ct)
        {
            return Apply(engine => engine.StepInto());
        }
    }

    public class StepOverExecutionHandler : ExecutionCommandHandler, IRequestHandler<StepOverExecutionCommand, ResultDto<bool>>
    {
        public StepOverExecutionHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> Handle(StepOverExecutionCommand request, CancellationToken ct)
        {
            return Apply(engine => engine.StepOver());
        }
    }

    public class SetExecutionBreakpointsHandler : ExecutionCommandHandler, IRequestHandler<SetExecutionBreakpointsCommand, ResultDto<bool>>
    {
        public SetExecutionBreakpointsHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> Handle(SetExecutionBreakpointsCommand request, CancellationToken ct)
        {
            return Apply(engine => engine.SetBreakpoints(request.flowStepIds));
        }
    }
}
