using Core.Enums;

namespace Core.Models.Dtos.Database
{
    /// <summary>
    /// One flow's run history in the shape the executions list reads it: enough to say whether the
    /// flow is healthy without opening a single run.
    /// </summary>
    public class FlowExecutionSummaryDto
    {
        public int FlowId { get; set; }
        public string FlowName { get; set; } = string.Empty;
        public bool IsSubFlow { get; set; }

        public int RunCount { get; set; }
        public int CompletedCount { get; set; }

        public DateTime? LastRunOn { get; set; }
        public ExecutionStatusEnum? LastStatus { get; set; }

        /// <summary>Oldest first, so a row of bars reads left to right like a timeline.</summary>
        public List<ExecutionStatusEnum> RecentOutcomes { get; set; } = new List<ExecutionStatusEnum>();
    }
}
