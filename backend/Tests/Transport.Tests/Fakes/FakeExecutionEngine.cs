using Business.Executions;
using Core.Enums;
using Core.Models.Dtos;

namespace Transport.Tests.Fakes
{
    /// <summary>Records whether it was asked to start, and starts nothing.</summary>
    public sealed class FakeExecutionEngine : IExecutionEngine
    {
        public bool WasStarted { get; private set; }

        public RunStateEnum State { get; } = RunStateEnum.FINISHED;
        public int ExecutionId { get; } = 1;
        public int FlowId { get; private set; }
        public bool IsRunning { get; }

        public Task<int> StartAsync(ExecutionStartDto dto, CancellationToken ct)
        {
            WasStarted = true;
            FlowId = dto.FlowId;
            return Task.FromResult(ExecutionId);
        }

        public void Stop()
        {
        }

        public void Pause()
        {
        }

        public void Continue()
        {
        }

        public void StepInto()
        {
        }

        public void StepOver()
        {
        }

        public void SetBreakpoints(IEnumerable<int> flowStepIds)
        {
        }
    }
}
