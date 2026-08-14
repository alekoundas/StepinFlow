using System.Collections.ObjectModel;

namespace Core.Models.Dtos
{
    public class FlowDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OrderNumber { get; set; }

        public IEnumerable<FlowStepDto> FlowSteps { get; set; } = new Collection<FlowStepDto>();
        public IEnumerable<FlowAreaDto> FlowAreas { get; set; } = new Collection<FlowAreaDto>();
        public IEnumerable<FlowPointDto> FlowPoints { get; set; } = new Collection<FlowPointDto>();
    }
}
