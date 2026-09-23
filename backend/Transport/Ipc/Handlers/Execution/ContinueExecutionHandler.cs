using Business.Services.ExecutionService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class ContinueExecutionHandler : ExecutionCommandHandler
    {
        public ContinueExecutionHandler(IExecutionEngine executionEngine) : base(executionEngine) { }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            return Apply(engine => engine.Continue());
        }
    }
}
