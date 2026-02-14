using Core.Enums;

namespace Core.Models.Database
{
    public class FlowStepImage : BaseDbModel
    {
        // IMAGE_LOCATION_EXTRACT
        public TemplateMatchModeEnum? TemplateMatchMode { get; set; }
        public byte[]? TemplateImage { get; set; }
        public float Accuracy { get; set; }
        public bool LoopOnMultipleFindings { get; set; }


        public int FlowStepId { get; set; }
        public FlowStep FlowStep { get; set; } = null!;
    }
}
