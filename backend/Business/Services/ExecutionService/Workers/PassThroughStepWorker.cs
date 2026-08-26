using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.ExecutionService.Workers
{
    /// <summary>
    /// For the step types that only exist to shape the tree - Success, Failure, Loop, Go To,Sub-Flow. 
    /// The navigator does the work, there is nothing to perform.
    /// </summary>
    public class PassThroughStepWorker : IStepWorker
    {
        public Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            return Task.FromResult(ExecutionStep.Success());
        }
    }
}
