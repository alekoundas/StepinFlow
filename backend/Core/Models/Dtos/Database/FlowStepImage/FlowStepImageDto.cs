using Core.Enums;

namespace Core.Models.Dtos
{
    public class FlowStepImageDto
    {
        public int Id { get; set; }


        public TemplateMatchModeEnum? TemplateMatchMode { get; set; }
        public byte[]? TemplateImage { get; set; }
        public float Accuracy { get; set; }
        public bool LoopOnMultipleFindings { get; set; }


        public int FlowStepId { get; set; }
        public FlowStepDto FlowStep { get; set; } = null!;
    }
}
