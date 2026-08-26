using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.ExecutionService.Workers
{
    public class WaitStepWorker : IStepWorker
    {
        private static readonly Random _random = new Random();

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            int milliseconds = step.WaitForMilliseconds;

            if (step.WaitForMillisecondsMax > step.WaitForMilliseconds)
                milliseconds = _random.Next(step.WaitForMilliseconds, step.WaitForMillisecondsMax);

            // The token is what makes pausing feel immediate instead of up to a few minutes late.
            await Task.Delay(milliseconds, ct);

            return ExecutionStep.Success();
        }
    }
}
