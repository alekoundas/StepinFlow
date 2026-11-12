using Business.Executions;
using Core.Enums;
using Core.Models.Dtos;

namespace Business.Tests.Fakes
{
    /// <summary>Whether a flow is executing, in what state, and which debugger commands it was given.</summary>
    public sealed class FakeExecutionEngine : IExecutionEngine
    {
        public bool IsRunning { get; set; }

        public RunStateEnum State { get; set; } = RunStateEnum.FINISHED;

        public List<string> Commands { get; } = new List<string>();

        public int ExecutionId { get; set; }

        public int FlowId { get; set; }

        /// <summary>The flows the execution going now uses, its own among them.</summary>
        public HashSet<int> ExecutingFlowIds { get; } = new HashSet<int>();

        public int StepCount
        {
            get { return 0; }
        }

        public Task Completion
        {
            get { return Task.CompletedTask; }
        }

        public Task<int> StartAsync(ExecutionStartDto dto, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            Commands.Add(nameof(Stop));
        }

        public void Pause()
        {
            Commands.Add(nameof(Pause));
        }

        public void Continue()
        {
            Commands.Add(nameof(Continue));
        }

        public void StepInto()
        {
            Commands.Add(nameof(StepInto));
        }

        public void StepOver()
        {
            Commands.Add(nameof(StepOver));
        }

        public void SetBreakpoints(IEnumerable<int> flowStepIds)
        {
            throw new NotImplementedException();
        }

        public bool IsExecuting(int flowId)
        {
            return IsRunning && ExecutingFlowIds.Contains(flowId);
        }
    }
}
