namespace Core.Models.Dtos.FlowScript
{
    /// <summary>
    /// A step as its flow's script would print it, and the names of what it points at, which the
    /// form has only as ids.
    /// </summary>
    public class FlowStepScriptLineDto
    {
        /// <summary>The comment's lines, then the step's line.</summary>
        public string Script { get; set; } = string.Empty;

        /// <summary>What a step named by its settings is called, before a number makes it unique.</summary>
        public string Name { get; set; } = string.Empty;

        public string? AreaName { get; set; }
        public string? PointName { get; set; }
        public string? PointEndName { get; set; }
        public string? ReferenceName { get; set; }
        public string? ReferenceEndName { get; set; }
        public string? SubFlowName { get; set; }
    }
}
