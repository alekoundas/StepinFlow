namespace Core.Models.Database
{
    /// <summary>
    /// A step's use of a template: the picture is the FlowTemplate's, the decisions about the
    /// search are the step's.
    /// </summary>
    public class FlowStepTemplate : BaseDbModel
    {
        public int OrderNumber { get; set; }

        public bool IsRequired { get; set; }

        public float Accuracy { get; set; } = 0.8f;


        public int FlowStepId { get; set; }
        public FlowStep FlowStep { get; set; } = null!;

        public int FlowTemplateId { get; set; }
        public FlowTemplate FlowTemplate { get; set; } = null!;
    }
}
