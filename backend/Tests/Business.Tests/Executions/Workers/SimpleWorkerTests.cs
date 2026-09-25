using System.Drawing;

using Business.Executions;
using Business.Executions.Workers;
using Core.Enums;
using Core.Models.Database;

using Microsoft.Extensions.Time.Testing;

namespace Business.Tests.Executions.Workers
{
    public sealed class SimpleWorkerTests
    {
        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        // ================================================================
        // Check Value
        // ================================================================

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

        // ================================================================
        // Pass-through and End Execution
        // ================================================================

        [Fact]
        public async Task A_structural_step_passes_and_does_nothing()
        {
            FlowStep loop = new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.LOOP };

            ExecutionStep result = await new PassThroughStepWorker().ExecuteAsync(loop, await WorkerCache.ForAsync(loop), Ct);

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
        }

        [Theory]
        [InlineData(true, "Execution ended as a pass.")]
        [InlineData(false, "Execution ended as a failure.")]
        public async Task End_execution_with_no_message_says_which_way_it_ended(bool asSuccess, string message)
        {
            FlowStep end = new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.END_EXECUTION, EndExecutionAsSuccess = asSuccess };

            ExecutionStep result = await new EndExecutionStepWorker().ExecuteAsync(end, await WorkerCache.ForAsync(end), Ct);

            result.Message.ShouldBe(message);
        }

        [Fact]
        public async Task End_execution_fills_its_message_from_the_flow()
        {
            FlowStep read = Read();
            FlowStep end = new FlowStep { Id = 2, FlowStepType = FlowStepTypeEnum.END_EXECUTION, Message = "Total was {{Read the total}}" };
            ExecutionCacheService cache = await WorkerCache.ForAsync(read, end);
            cache.Ran(read, value: "42");

            ExecutionStep result = await new EndExecutionStepWorker().ExecuteAsync(end, cache, Ct);

            result.Message.ShouldBe("Total was 42");
        }

        // ================================================================
        // Wait
        // ================================================================

        [Fact]
        public async Task A_wait_lasts_its_time_on_the_clock_and_not_a_moment_less()
        {
            FakeTimeProvider clock = new FakeTimeProvider();
            FlowStep wait = new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.WAIT, WaitForMilliseconds = 5000 };

            Task<ExecutionStep> waiting = new WaitStepWorker(clock).ExecuteAsync(wait, await WorkerCache.ForAsync(wait), Ct);

            clock.Advance(TimeSpan.FromMilliseconds(4999));
            waiting.IsCompleted.ShouldBeFalse();

            clock.Advance(TimeSpan.FromMilliseconds(1));
            (await waiting).Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
        }

        [Fact]
        public async Task A_wait_with_a_range_lasts_somewhere_inside_it()
        {
            FakeTimeProvider clock = new FakeTimeProvider();
            FlowStep wait = new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.WAIT, WaitForMilliseconds = 800, WaitForMillisecondsMax = 1200 };

            Task<ExecutionStep> waiting = new WaitStepWorker(clock).ExecuteAsync(wait, await WorkerCache.ForAsync(wait), Ct);

            clock.Advance(TimeSpan.FromMilliseconds(799));
            waiting.IsCompleted.ShouldBeFalse();

            clock.Advance(TimeSpan.FromMilliseconds(401));
            (await waiting).Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
        }

        [Fact]
        public async Task Stopping_ends_a_wait_at_once()
        {
            using CancellationTokenSource stop = new CancellationTokenSource();
            FlowStep wait = new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.WAIT, WaitForMilliseconds = 60_000 };

            Task<ExecutionStep> waiting = new WaitStepWorker(new FakeTimeProvider()).ExecuteAsync(wait, await WorkerCache.ForAsync(wait), stop.Token);
            await stop.CancelAsync();

            await Should.ThrowAsync<TaskCanceledException>(waiting);
        }
    }
}
