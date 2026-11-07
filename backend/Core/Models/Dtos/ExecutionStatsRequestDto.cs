namespace Core.Models.Dtos
{
    public class ExecutionStatsRequestDto
    {
        /// <summary>One flow's executions only. Null for every flow's.</summary>
        public int? FlowId { get; set; }

        /// <summary>Only the executions started in the last this many days. Null for all of them.</summary>
        public int? Days { get; set; }
    }
}
