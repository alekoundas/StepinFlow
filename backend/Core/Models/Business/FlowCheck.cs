using Core.Enums;

namespace Core.Models.Business
{
    public class FlowCheck
    {
        public int FlowStepId { get; set; }
        public FlowStepTypeEnum FlowStepType { get; set; }

        public string Name { get; set; } = string.Empty;
        public string CodeComment { get; set; } = string.Empty;
        public string? MarkerName { get; set; }
        public bool IsFatal { get; set; }
        public string? FailureMessage { get; set; }
    }
}
