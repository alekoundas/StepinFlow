using System.Collections.ObjectModel;

namespace Core.Models.Dtos
{
    public class FlowDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OrderNumber { get; set; }

        public IEnumerable<FlowStepCreateDto> FlowSteps { get; set; } = new Collection<FlowStepCreateDto>();
        public IEnumerable<FlowSearchAreaCreateDto> FlowSearchAreas { get; set; } = new Collection<FlowSearchAreaCreateDto>();
    }
}
