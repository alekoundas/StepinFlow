using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    public class Flow : BaseDbModel
    {
        // Flow public identifier (stays the same across versions)
        public Guid PublicId { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsSubFlow { get; set; }

        // A QA test rather than an automation, chosen at the recording setup.
        public bool IsQaTest { get; set; }


        public IEnumerable<FlowStep> FlowSteps { get; set; } = new Collection<FlowStep>();
        public IEnumerable<FlowArea> FlowAreas { get; set; } = new Collection<FlowArea>();
        public IEnumerable<FlowPoint> FlowPoints { get; set; } = new Collection<FlowPoint>();
        public IEnumerable<FlowViewport> FlowViewports { get; set; } = new Collection<FlowViewport>();
        public IEnumerable<FlowCsvColumn> FlowCsvColumns { get; set; } = new Collection<FlowCsvColumn>();
        public IEnumerable<FlowTemplate> FlowTemplates { get; set; } = new Collection<FlowTemplate>();
    }
}
