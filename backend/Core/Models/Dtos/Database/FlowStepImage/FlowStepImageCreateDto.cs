using Core.Enums;

namespace Core.Models.Dtos
{
    public class FlowStepImageCreateDto
    {
        public TemplateMatchModeEnum? TemplateMatchMode { get; set; }
        public byte[]? TemplateImage { get; set; }
        public float Accuracy { get; set; }
        public bool LoopOnMultipleFindings { get; set; }


        public int FlowStepId { get; set; }
        public FlowStepCreateDto FlowStep { get; set; } = null!;
    }
}
