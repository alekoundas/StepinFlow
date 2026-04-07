using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    public class FlowSearchArea : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;

        public FlowSearchAreaTypeEnum FlowSearchAreaType{ get; set; }

        public string ApplicationName { get; set; } = string.Empty;
        public string MonitorIndex { get; set; } = string.Empty;

        // Custom search area
        public int LocationX { get; set; }
        public int LocationY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        // Flow
        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;

        public IEnumerable<FlowStep> FlowSteps { get; set; } = new Collection<FlowStep>();
    }
}
