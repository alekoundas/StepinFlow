using System.Collections.ObjectModel;

namespace Core.Models.Dtos
{
    public class FlowCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public int? OrderNumber { get; set; }

        //public IEnumerable<FlowStepDto> FlowSteps { get; set; } = new Collection<FlowStepDto>();
        public IEnumerable<FlowSearchAreaDto> FlowSearchAreas { get; set; } = new Collection<FlowSearchAreaDto>();
    }
}
