using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Executions
{
    /// <summary>
    /// What an execution leaves behind. Every method does nothing when history is off, so turning it
    /// off changes what gets stored and never what a flow does.
    /// </summary>
    public interface IExecutionHistoryService
    {
        int ExecutionId { get; }

        Task<int> StartAsync(ExecutionStartDto dto, IReadOnlyDictionary<int, FlowStep> stepsById, CancellationToken ct);
        Task RecordAsync(ExecutionStep executionStep);

        /// <summary>Writes the steps still waiting for their batch, so a paused execution shows every step it executed.</summary>
        Task FlushAsync();

        Task CompleteAsync(ExecutionStatusEnum status, string error, int? errorFlowStepId, int stepCount);

        /// <summary>The screenshot behind a saved step of the execution going now, while it is one of the last ones kept. Null otherwise.</summary>
        byte[]? GetKeptScreenshot(int executionStepId);
    }
}
