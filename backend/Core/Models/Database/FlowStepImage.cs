using Core.Enums;

namespace Core.Models.Database
{
    /// <summary>
    /// One template an IMAGE_SEARCH step looks for. The blob lives here rather than on FlowStep so
    /// a tree or list query never drags megabytes of PNG along.
    /// </summary>
    public class FlowStepImage : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;
        public int OrderNumber { get; set; }

        /// <summary>PNG. Never JPEG: the artifacts wreck normalized matching.</summary>
        public byte[]? TemplateImage { get; set; }

        /// <summary>
        /// Small PNG of the template, so the tree can show what a step looks for without loading
        /// the full blob. Also PNG: the eraser leaves transparent pixels and JPEG has no alpha.
        /// </summary>
        public byte[]? Thumbnail { get; set; }

        /// <summary>
        /// With no image marked required the step succeeds on any match, which is what you want
        /// for several variants of the same icon. Mark one required and it must be found.
        /// </summary>
        public bool IsRequired { get; set; }

        // Null means "use the step's setting". A crisp icon and anti-aliased text want
        // different thresholds.
        public TemplateMatchModeEnum? TemplateMatchMode { get; set; }
        public float? Accuracy { get; set; }

        // Where to click within the template, in template pixels from its top left.
        // Scaled by the same ratio as the template at match time.
        public int ClickOffsetX { get; set; }
        public int ClickOffsetY { get; set; }

        // Size of the frame this template was captured in, which is the scaling key. The monitor
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
