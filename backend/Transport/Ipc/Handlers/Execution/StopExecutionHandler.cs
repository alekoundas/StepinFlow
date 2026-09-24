using Business.Executions;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class StopExecutionHandler : ExecutionCommandHandler
    {
        public StopExecutionHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            return Apply(engine => engine.Stop());
        }
    }
}
