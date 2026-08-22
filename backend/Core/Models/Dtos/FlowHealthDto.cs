namespace Core.Models.Dtos
{
    public class FlowHealthRequestDto
    {
        public List<int> FlowIds { get; set; } = new List<int>();
    }

    /// <summary>Counts only. The messages belong on the flow, not in a list.</summary>
    public class FlowHealthDto
    {
        public int FlowId { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
    }
}
