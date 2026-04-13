using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    public class FlowSearchArea : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;
        public FlowSearchAreaTypeEnum Type{ get; set; }


        public string AppWindowName { get; set; } = string.Empty;
        public string MonitorUniqueId { get; set; } = string.Empty;

        // Custom search area
        public int LocationX { get; set; }
        public int LocationY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        // Flow
        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;

        public IEnumerable<FlowStep> FlowSteps { get; set; } = [];
    }
}
