using System.Collections.ObjectModel;

namespace Core.Models.Dtos
{
    public class FlowDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSubFlow { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        // Counts for the list, projected rather than loaded. The collections below stay empty
        // there; only the form asks for them.
        public int StepCount { get; set; }
        public int AreaCount { get; set; }
        public int PointCount { get; set; }

        /// <summary>Sub-flows only: how many distinct flows invoke this one.</summary>
        public int CallerCount { get; set; }

        public IEnumerable<FlowStepDto> FlowSteps { get; set; } = new Collection<FlowStepDto>();
        public IEnumerable<FlowAreaDto> FlowAreas { get; set; } = new Collection<FlowAreaDto>();
        public IEnumerable<FlowPointDto> FlowPoints { get; set; } = new Collection<FlowPointDto>();
    }
}
