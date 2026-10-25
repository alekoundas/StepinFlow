using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>A flow, or a step and the flow it is in, that the header's search found.</summary>
    public class FlowSearchHitDto
    {
        public int FlowId { get; set; }
        public string FlowName { get; set; } = string.Empty;
        public bool IsSubFlow { get; set; }
        public bool IsQaTest { get; set; }

        /// <summary>Null when the flow itself matched.</summary>
        public int? FlowStepId { get; set; }
        public string? FlowStepName { get; set; }
        public FlowStepTypeEnum? FlowStepType { get; set; }
    }
}
