using Core.Enums;

namespace Core.Models.Database
{
    public class FlowLocation : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;

        // Frame this point lives in. Null = absolute screen coordinates.
        public int? FlowSearchAreaId { get; set; }
        public FlowSearchArea? FlowSearchArea { get; set; }

        public AreaSizingModeEnum OffsetMode { get; set; }

        // ABSOLUTE_PX: pixels from the anchor.
        public int LocationX { get; set; }
        public int LocationY { get; set; }

        // RATIO: 0..1 of the frame.
        public float RatioX { get; set; }
        public float RatioY { get; set; }

        // Flow
        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;

        // Steps using this location as their primary point.
        public IEnumerable<FlowStep> FlowSteps { get; set; } = [];

        // Steps using this location as their end point (CURSOR_DRAG).
        public IEnumerable<FlowStep> EndFlowSteps { get; set; } = [];
    }
}
