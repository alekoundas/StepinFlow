namespace Core.Models.Dtos
{
    public class FlowStepPasteDto
    {
        /// <summary>The copied step. Everything under it comes with it.</summary>
        public int FlowStepId { get; set; }

        public int? TargetParentFlowStepId { get; set; }
        public int? TargetFlowId { get; set; }

        public int TargetIndex { get; set; }
    }
}
