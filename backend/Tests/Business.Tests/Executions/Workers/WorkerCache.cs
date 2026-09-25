using System.Drawing;

using Business.Executions;
using Business.Tests.Fakes;
using Core.Models.Database;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// The real execution cache, with the flow's steps in it and nothing run yet. Screenshots are
    /// not kept, so it never needs the screenshot service or the settings.
    /// </summary>
    public static class WorkerCache
    {
        public static async Task<ExecutionCacheService> ForAsync(params FlowStep[] steps)
        {
            ExecutionCacheService cache = new ExecutionCacheService(null!, new FakeScreenshotService(), TimeProvider.System);
            await cache.ResetAsync(steps.ToDictionary(x => x.Id), keepsScreenshots: false, TestContext.Current.CancellationToken);
            return cache;
        }

        // What an earlier step left behind, as the walker would have recorded it.
        public static void Ran(this ExecutionCacheService cache, FlowStep step, string? value = null, Point? location = null)
        {
            ExecutionStep result = ExecutionStep.Success(location);
            result.Value = value;
            cache.RecordExecutionStep(step.Id, result);
        }
    }
}
