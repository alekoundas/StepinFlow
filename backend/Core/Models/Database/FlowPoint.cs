using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    public class FlowPoint : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;
        public AreaSizingModeEnum OffsetMode { get; set; }
        public int AuthoredDpi { get; set; } // The DPI those pixels were captured at, so inside a DPI area they follow the monitor.


        // ABSOLUTE_PX: pixels from the anchor.
        public int LocationX { get; set; }
        public int LocationY { get; set; }


        // RATIO: 0..1 of the area.
        public float RatioX { get; set; }
        public float RatioY { get; set; }



        // Flow
        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;

        // FlowArea
        public int? FlowAreaId { get; set; }
        public FlowArea? FlowArea { get; set; }


        public IEnumerable<FlowStep> FlowSteps { get; set; } = new Collection<FlowStep>();
        public IEnumerable<FlowStep> EndFlowSteps { get; set; } = new Collection<FlowStep>();
    }
}
