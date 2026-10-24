namespace Core.Models.Dtos
{
    public class FlowStepTemplateDto
    {
        public int Id { get; set; }

        public int OrderNumber { get; set; }

        public bool IsRequired { get; set; }

        public float Accuracy { get; set; } = 0.8f;

        public int FlowStepId { get; set; }

        /// <summary>0 with a FlowTemplate to save as a new picture of the flow.</summary>
        public int FlowTemplateId { get; set; }
        public FlowTemplateDto? FlowTemplate { get; set; }
    }
}
