namespace Core.Models.Database
{
    public class FlowLocation : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;

        // Location in physical (real device) pixels, same space as FlowSearchArea.
        public int LocationX { get; set; }
        public int LocationY { get; set; }

        // Flow
        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;

        // Steps using this location as their primary point.
        public IEnumerable<FlowStep> FlowSteps { get; set; } = [];

        // Steps using this location as their end point (CURSOR_DRAG).
        public IEnumerable<FlowStep> EndFlowSteps { get; set; } = [];
    }
}
