using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    public class FlowPoint : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;

        // Area this point lives in. Null = absolute screen coordinates.
        public int? FlowAreaId { get; set; }
        public FlowArea? FlowArea { get; set; }

        public AreaSizingModeEnum OffsetMode { get; set; }

        // ABSOLUTE_PX: pixels from the anchor.
        public int LocationX { get; set; }
        public int LocationY { get; set; }

        // RATIO: 0..1 of the area.
        public float RatioX { get; set; }
        public float RatioY { get; set; }

        // Flow
        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;

        // Steps using this location as their primary point.
        public IEnumerable<FlowStep> FlowSteps { get; set; } = new Collection<FlowStep>();

        // Steps using this location as their end point (CURSOR_DRAG).
        public IEnumerable<FlowStep> EndFlowSteps { get; set; } = new Collection<FlowStep>();
    }
}
