using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    public class SubFlow : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;
        public int OrderNumber { get; set; }

        public virtual IEnumerable<FlowStep> FlowSteps { get; set; } = new Collection<FlowStep>();
        public virtual IEnumerable<FlowSearchArea> FlowSearchAreas { get; set; } = new Collection<FlowSearchArea>();
}
}
