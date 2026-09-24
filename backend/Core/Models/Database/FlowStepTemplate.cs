namespace Core.Models.Database
{
    public class FlowStepTemplate : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;
        public int OrderNumber { get; set; }

        public byte[]? TemplateImage { get; set; } // PNG
        public byte[]? Thumbnail { get; set; } //todo maybe drop

        public bool IsRequired { get; set; }

        public float Accuracy { get; set; } = 0.8f;

        public int ClickOffsetX { get; set; }
        public int ClickOffsetY { get; set; }

        // The size of the area this template was captured in, and the DPI it was captured at - the two things a different screen can change about it.
        public int AuthoredFlowAreaWidth { get; set; }
        public int AuthoredFlowAreaHeight { get; set; }
        public int AuthoredDpi { get; set; }


        public int FlowStepId { get; set; }
        public FlowStep FlowStep { get; set; } = null!;
    }
}
