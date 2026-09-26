using System.Drawing;

using Business.Executions;
using Business.Executions.Workers;
using Core.Enums;
using Core.Models.Database;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// The only step whose whole job is a decision. It reads what an earlier step produced and
    /// branches on it, so what matters is that the value it judged travels out with the result -
    /// a failure that does not say what it read is a failure nobody can diagnose.
    /// </summary>
    public sealed class CheckValueStepWorkerTests
    {
        private static FlowStep Read(string name = "Read the total")
        {
            return new FlowStep { Id = 1, Name = name, FlowStepType = FlowStepTypeEnum.SEARCH_TEXT };
        }

        private static FlowStep Check(ConditionTypeEnum condition, string text, string textEnd = "")
        {
            return new FlowStep { Id = 2, Name = "Check", FlowStepType = FlowStepTypeEnum.CHECK_VALUE, FlowStepReferenceId = 1, ConditionType = condition, ConditionText = text, ConditionTextEnd = textEnd };
        }

        [Fact]
        public async Task A_value_that_satisfies_the_condition_passes_and_carries_on_down()
        {
            FlowStep read = Read();
            FlowStep check = Check(ConditionTypeEnum.GREATER_THAN, "100");
            ExecutionCacheService cache = await WorkerCache.ForAsync(read, check);
            cache.Ran(read, value: "142", location: new Point(5, 6));

            ExecutionStep result = await new CheckValueStepWorker().ExecuteAsync(check, cache, Ct);

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            result.Value.ShouldBe("142");
            result.Location.ShouldBe(new Point(5, 6));
        }

        [Fact]
        public async Task A_value_that_does_not_satisfy_it_fails_saying_what_was_read()
        {
            FlowStep read = Read();
            FlowStep check = Check(ConditionTypeEnum.GREATER_THAN, "100");
            ExecutionCacheService cache = await WorkerCache.ForAsync(read, check);
            cache.Ran(read, value: "42");

            ExecutionStep result = await new CheckValueStepWorker().ExecuteAsync(check, cache, Ct);

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Value.ShouldBe("42");
            result.Message.ShouldBe(@"""42"" does not satisfy GREATER_THAN 100.");
        }

        [Fact]
        public async Task The_expected_value_can_be_another_steps_result()
        {
            FlowStep limit = new FlowStep { Id = 3, Name = "Read the limit", FlowStepType = FlowStepTypeEnum.SEARCH_TEXT };
            FlowStep read = Read();
            FlowStep check = Check(ConditionTypeEnum.LESS_THAN, "{{Read the limit}}");
            ExecutionCacheService cache = await WorkerCache.ForAsync(read, check, limit);
            cache.Ran(read, value: "42");
            cache.Ran(limit, value: "50");

            ExecutionStep result = await new CheckValueStepWorker().ExecuteAsync(check, cache, Ct);

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
        }

        [Fact]
        public async Task A_variable_with_no_value_fails_naming_it()
        {
            FlowStep read = Read();
            FlowStep check = Check(ConditionTypeEnum.EQUALS, "{{expected}}");
            ExecutionCacheService cache = await WorkerCache.ForAsync(read, check);
            cache.Ran(read, value: "42");

            ExecutionStep result = await new CheckValueStepWorker().ExecuteAsync(check, cache, Ct);

            result.Outcome.ShouldBe(StepOutcomeEnum.FAILURE);
            result.Message.ShouldBe("Nothing has a value for {{expected}}.");
        }

        [Fact]
        public async Task Reading_a_step_that_has_not_run_fails()
        {
            FlowStep check = Check(ConditionTypeEnum.IS_EMPTY, string.Empty);
            ExecutionCacheService cache = await WorkerCache.ForAsync(Read(), check);

            ExecutionStep result = await new CheckValueStepWorker().ExecuteAsync(check, cache, Ct);

            result.Message.ShouldBe("The step this reads from has not run.");
        }
    }
}
