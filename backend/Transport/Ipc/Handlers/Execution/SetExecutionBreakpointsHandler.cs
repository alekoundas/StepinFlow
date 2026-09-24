using Business.Executions;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SetExecutionBreakpointsHandler : ExecutionCommandHandler
    {
        public SetExecutionBreakpointsHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> HandleAsync(List<int> flowStepIds, CancellationToken ct)
        {
            return Apply(engine => engine.SetBreakpoints(flowStepIds));
        }
    }
}
