using Business.Executions;
using Business.Executions.Workers;
using Core.Enums;
using Core.Models.Database;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// The step that declares a verdict, which is the only way a flow says it passed or failed. Its
    /// message is what a report and a notification quote, so it is never allowed to be empty - a
    /// verdict nobody can read is the failure INCONCLUSIVE exists to prevent.
    /// </summary>
    public sealed class EndExecutionStepWorkerTests
    {
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
            FlowStep read = new FlowStep { Id = 1, Name = "Read the total", FlowStepType = FlowStepTypeEnum.SEARCH_TEXT };
            FlowStep end = new FlowStep { Id = 2, FlowStepType = FlowStepTypeEnum.END_EXECUTION, Message = "Total was {{Read the total}}" };
            ExecutionCacheService cache = await WorkerCache.ForAsync(read, end);
            cache.Ran(read, value: "42");

            ExecutionStep result = await new EndExecutionStepWorker().ExecuteAsync(end, cache, Ct);

            result.Message.ShouldBe("Total was 42");
        }
    }
}
