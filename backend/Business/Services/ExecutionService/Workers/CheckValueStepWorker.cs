
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.ExecutionService.Workers
{
    /// <summary>
    /// Tests what an earlier step produced.
    /// </summary>
    public class CheckValueStepWorker : IStepWorker
    {
        public Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            // Validate.
            if (step.FlowStepReferenceId == null)
                return Task.FromResult(ExecutionStep.Failure("There is no step to read a value from."));

            ExecutionStep? source = cache.GetExecutionStepFrom(step.FlowStepReferenceId.Value);
            if (source == null)
                return Task.FromResult(ExecutionStep.Failure("The step this reads from has not run."));

            // Execute.
            string value = source.Value ?? string.Empty;
            VariableTranslationResult expected = cache.ResolveVariables(step.ConditionText);
            VariableTranslationResult expectedEnd = cache.ResolveVariables(step.ConditionTextEnd);

            if (!expected.IsResolved || !expectedEnd.IsResolved)
                return Task.FromResult(ExecutionStep.Failure(VariableTranslator.DescribeUnresolved([.. expected.Unresolved, .. expectedEnd.Unresolved])));

            bool satisfied = ConditionEvaluator.IsSatisfied(value, step.ConditionType, expected.Text, expectedEnd.Text);

            if (!satisfied)
            {
                ExecutionStep unsatisfied = ExecutionStep.Failure($"\"{value}\" does not satisfy {ConditionEvaluator.Describe(step)}.");
                unsatisfied.Value = value;
                return Task.FromResult(unsatisfied);
            }

            // The value carries on down, so a chain of checks all read the step that produced it.
            ExecutionStep checkedValue = ExecutionStep.Success(source.Location);
            checkedValue.Value = value;
            return Task.FromResult(checkedValue);
        }


    }
}
