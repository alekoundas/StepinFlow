using Business.Executions.Workers;
using Core.Enums;
using Core.Models.Database;

using Microsoft.Extensions.Time.Testing;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// Twenty-six lines, and the reason the worker takes a <see cref="TimeProvider"/> at all: with
    /// the wall clock a test of a five-second wait took five seconds, and "somewhere between 800ms
    /// and 1200ms" could not be asserted. The clock is moved by hand here instead.
    /// </summary>
    public sealed class WaitStepWorkerTests
    {
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
