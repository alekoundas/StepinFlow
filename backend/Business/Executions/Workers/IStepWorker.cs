using Core.Models.Database;

namespace Business.Executions.Workers
{
    public interface IStepWorker
    {
        Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct);
    }
}
