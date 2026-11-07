using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>A page of the executions feed: every flow's executions, or one flow's, newest first.</summary>
    public class ExecutionFeedRequestDto
    {
        /// <summary>One flow's executions only. Null for every flow's.</summary>
        public int? FlowId { get; set; }

        /// <summary>Only executions older than this one, which is where the page before ended. Null for the newest.</summary>
        public int? BeforeId { get; set; }

        /// <summary>Only executions that ended one of these ways. Empty for all of them.</summary>
        public List<ExecutionStatusEnum> Statuses { get; set; } = new List<ExecutionStatusEnum>();
    }
}
