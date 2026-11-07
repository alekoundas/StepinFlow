namespace Core.Models.Database
{
    public class FlowStepTemplate : BaseDbModel
    {
        public int OrderNumber { get; set; }
        public float Accuracy { get; set; } = 0.8f;


        public int FlowStepId { get; set; }
        public FlowStep FlowStep { get; set; } = null!;

        public int FlowTemplateId { get; set; }
        public FlowTemplate FlowTemplate { get; set; } = null!;
    }
}
