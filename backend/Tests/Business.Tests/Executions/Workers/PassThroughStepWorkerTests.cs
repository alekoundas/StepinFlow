using Business.Executions.Workers;
using Core.Enums;
using Core.Models.Database;
using static Business.Tests.TestToken;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// The worker for the step types the walker handles itself - LOOP, GO_TO, SUB_FLOW, SUCCESS,
    /// FAILURE, MARKER. It exists so those types have something to execute, and it does nothing.
    ///
    /// It is also the fallback for a type nobody mapped, which is the hazard: a new step type with a
    /// forgotten registration runs as a no-op that reports success. The startup check for that is in
    /// TODO.md, and it belongs here when it lands.
    /// </summary>
    public sealed class PassThroughStepWorkerTests
    {
        [Fact]
        public async Task A_structural_step_passes_and_does_nothing()
        {
            FlowStep loop = new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.LOOP };

            ExecutionStep result = await new PassThroughStepWorker().ExecuteAsync(loop, await WorkerCache.ForAsync(loop), Ct);

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
        }
    }
}
