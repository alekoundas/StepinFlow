using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    public class Flow : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsSubFlow { get; set; }


        // The application this flow tests. Sizing a viewport needs a window to size, and guessing
        // it from whatever has focus is what breaks on another machine.
        public int? AppUnderTestAreaId { get; set; }
        public FlowArea? AppUnderTestArea { get; set; }
        public AppCloseModeEnum AppCloseMode { get; set; }


        public IEnumerable<FlowStep> FlowSteps { get; set; } = new Collection<FlowStep>();
        public IEnumerable<FlowArea> FlowAreas { get; set; } = new Collection<FlowArea>();
        public IEnumerable<FlowPoint> FlowPoints { get; set; } = new Collection<FlowPoint>();
        public IEnumerable<FlowViewport> FlowViewports { get; set; } = new Collection<FlowViewport>();
        public IEnumerable<FlowCsvColumn> FlowCsvColumns { get; set; } = new Collection<FlowCsvColumn>();
    }
}
