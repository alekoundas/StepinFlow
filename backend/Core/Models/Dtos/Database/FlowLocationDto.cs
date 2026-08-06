namespace Core.Models.Dtos
{
    public class FlowLocationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Location in physical (real device) pixels.
        public int LocationX { get; set; }
        public int LocationY { get; set; }

        // Flow
        public int FlowId { get; set; }

        // How many FlowSteps use this location (primary + end). Read only, projected by the query.
        public int FlowStepsCount { get; set; }
    }
}
