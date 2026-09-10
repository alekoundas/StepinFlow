using Core.Models.Database;

using Core.Helpers;
using Core.Models.Business;

namespace Business.Services.ExecutionService.Workers
{
    /// <summary>
    /// Stops the execution and stamps its verdict.
    ///
    /// The step itself does nothing: the engine reads the type and stops walking. It is a step
    /// rather than a flag so that the cleanup before it - log out, empty the basket, notify - is
    /// visible in the flow, in the order it happens.
    /// </summary>
    public class EndExecutionStepWorker : IStepWorker
    {
        public Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(step.Message))
            {
                string fallback = step.EndExecutionAsSuccess ? "Execution ended as a pass." : "Execution ended as a failure.";

                return Task.FromResult(ExecutionStep.Success(message: fallback));
            }

            VariableTranslationResult message = cache.ResolveVariables(step.Message);

            return Task.FromResult(ExecutionStep.Success(message: message.Text));
        }
    }
}
