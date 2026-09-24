using Core.Enums;
using Core.Models.Dtos;

namespace Business.Services.ExecutionService
{
    public interface IExecutionEngine
    {
        RunStateEnum State { get; }
        int ExecutionId { get; }
        int FlowId { get; }
        bool IsRunning { get; }

        Task<int> StartAsync(ExecutionStartDto dto, CancellationToken ct);

        void Stop();
        void Pause();
        void Continue();
        void StepInto();
        void StepOver();
        void SetBreakpoints(IEnumerable<int> flowStepIds);
    }
}
