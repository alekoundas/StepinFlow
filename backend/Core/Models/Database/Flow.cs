using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    public class Flow : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;
        public int OrderNumber { get; set; }

        public IEnumerable<FlowStep> FlowSteps { get; set; } = new Collection<FlowStep>();
        public IEnumerable<FlowSearchArea> FlowSearchAreas { get; set; } = new Collection<FlowSearchArea>();
}
}
