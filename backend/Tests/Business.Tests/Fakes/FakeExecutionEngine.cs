using Business.Executions;
using Core.Enums;
using Core.Models.Dtos;

namespace Business.Tests.Fakes
{
    /// <summary>Only whether a flow is executing.</summary>
    public sealed class FakeExecutionEngine : IExecutionEngine
    {
        public bool IsRunning { get; set; }

        public RunStateEnum State
        {
            get { return RunStateEnum.FINISHED; }
        }

        public int ExecutionId
        {
            get { return 0; }
        }

        public int FlowId
        {
            get { return 0; }
        }

        public Task<int> StartAsync(ExecutionStartDto dto, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Continue()
        {
            throw new NotImplementedException();
        }

        public void StepInto()
        {
            throw new NotImplementedException();
        }

        public void StepOver()
        {
            throw new NotImplementedException();
        }

        public void SetBreakpoints(IEnumerable<int> flowStepIds)
        {
            throw new NotImplementedException();
        }
    }
}
