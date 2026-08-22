using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    public class Flow : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsSubFlow { get; set; }

        public IEnumerable<FlowStep> FlowSteps { get; set; } = new Collection<FlowStep>();
        public IEnumerable<FlowArea> FlowAreas { get; set; } = new Collection<FlowArea>();
        public IEnumerable<FlowPoint> FlowPoints { get; set; } = new Collection<FlowPoint>();
    }
}
