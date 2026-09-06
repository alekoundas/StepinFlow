using Core.Enums;

namespace Core.Models.Database
{
    public class FlowStepTemplate : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;
        public int OrderNumber { get; set; }

        public TemplateMatchModeEnum? TemplateMatchMode { get; set; }

        public byte[]? TemplateImage { get; set; } // PNG
        public byte[]? Thumbnail { get; set; } //todo maybe drop

        public bool IsRequired { get; set; }
        public float? Accuracy { get; set; }

        public int ClickOffsetX { get; set; }
        public int ClickOffsetY { get; set; }

        // Size of the area this template was captured in, which is the scaling key. The monitor
        // fields are diagnostics for the "your setup differs" warning, not maths.
        public int AuthoredFrameWidth { get; set; }
        public int AuthoredFrameHeight { get; set; }
        public string AuthoredMonitorId { get; set; } = string.Empty;
        public int AuthoredMonitorDpi { get; set; }

        public bool AllowMultiScale { get; set; }
        public float ScaleTolerance { get; set; } = 0.15f;


        public int FlowStepId { get; set; }
        public FlowStep FlowStep { get; set; } = null!;
    }
}
