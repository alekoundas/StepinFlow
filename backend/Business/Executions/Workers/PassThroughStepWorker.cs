using Core.Enums;
using Core.Models.Database;

namespace Business.Executions.Workers
{
    /// <summary>
    /// For the step types that only exist to shape the tree - Success, Failure, Loop, Break, Sub-Flow.
    /// The navigator does the work, there is nothing to perform.
    /// </summary>
    public class PassThroughStepWorker : IStepWorker
    {
        public Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            // Says which step it left, so the execution reads without opening the flow.
            if (step.FlowStepType == FlowStepTypeEnum.BREAK && step.FlowStepReferenceId is int targetId && cache.StepsById.TryGetValue(targetId, out FlowStep? target))
                return Task.FromResult(ExecutionStep.Success(message: $"Broke out of \"{target.Name}\"."));

            return Task.FromResult(ExecutionStep.Success());
        }
    }
}
