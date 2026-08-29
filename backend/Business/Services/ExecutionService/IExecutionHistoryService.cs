using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Services.ExecutionService
{
    /// <summary>
    /// What a run leaves behind. Every method does nothing when history is off, so turning it off
    /// changes what gets stored and never what a flow does.
    /// </summary>
    public interface IExecutionHistoryService
    {
        int ExecutionId { get; }

        Task<int> StartAsync(ExecutionStartDto dto, IReadOnlyDictionary<int, FlowStep> stepsById, CancellationToken ct);
        Task RecordAsync(ExecutionStep executionStep);
        Task CompleteAsync(ExecutionStatusEnum status, string error, int? errorFlowStepId, int stepCount);
    }
}
