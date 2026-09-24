using Business.Executions;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class StepOverExecutionHandler : ExecutionCommandHandler
    {
        public StepOverExecutionHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            return Apply(engine => engine.StepOver());
        }
    }
}
