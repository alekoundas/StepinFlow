using Core.Enums;

namespace Core.Models.Dtos
{
    public class FlowStepImageDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public int OrderNumber { get; set; }

        public byte[]? TemplateImage { get; set; }

        public bool IsRequired { get; set; }

        // Null means "use the step's setting".
        public TemplateMatchModeEnum? TemplateMatchMode { get; set; }
        public float? Accuracy { get; set; }

        public int ClickOffsetX { get; set; }
        public int ClickOffsetY { get; set; }

        public int AuthoredFrameWidth { get; set; }
        public int AuthoredFrameHeight { get; set; }
        public string AuthoredMonitorId { get; set; } = string.Empty;
        public int AuthoredMonitorDpi { get; set; }

        public bool AllowMultiScale { get; set; }
        public float ScaleTolerance { get; set; } = 0.15f;

        public int FlowStepId { get; set; }
    }
}
