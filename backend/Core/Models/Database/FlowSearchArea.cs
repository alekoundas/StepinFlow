using Core.Models.Database;
using System.Collections.ObjectModel;

namespace Core.Models
{
    public class FlowSearchArea : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;

        public int LocationLeft { get; set; }
        public int LocationTop { get; set; }
        public int LocationToRight { get; set; }
        public int LocationToBottom { get; set; }

        // Flow
        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;

        public IEnumerable<FlowStep> FlowSteps { get; set; } = new Collection<FlowStep>();
    }
}
