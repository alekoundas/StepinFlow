using Core.Enums;

namespace Core.Models.Business
{
    public class FlowCheckNode
    {
        public int Id { get; set; }
        public int? ParentFlowStepId { get; set; }
        public FlowStepTypeEnum FlowStepType { get; set; }
        public int OrderNumber { get; set; }

        public string Name { get; set; } = string.Empty;
        public string CodeComment { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public bool EndExecutionAsSuccess { get; set; }
    }
}
