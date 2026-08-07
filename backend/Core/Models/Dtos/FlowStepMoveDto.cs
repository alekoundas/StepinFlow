namespace Core.Models.Dtos
{
    public class FlowStepMoveDto
    {
        public int FlowStepId { get; set; }

        public int? TargetParentFlowStepId { get; set; }
        public int? TargetFlowId { get; set; }

        public int TargetIndex { get; set; }
    }
}
