using Core.Models.Database;

namespace Business.Executions.Workers
{
    public class WaitStepWorker : IStepWorker
    {
        private readonly TimeProvider _timeProvider;

        public WaitStepWorker(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            int milliseconds = step.WaitForMilliseconds;
            if (step.WaitForMillisecondsMax > step.WaitForMilliseconds)
                milliseconds = Random.Shared.Next(step.WaitForMilliseconds, step.WaitForMillisecondsMax);

            // The token is what makes pausing feel immediate instead of up to a few minutes late.
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), _timeProvider, ct);

            return ExecutionStep.Success();
        }
    }
}
