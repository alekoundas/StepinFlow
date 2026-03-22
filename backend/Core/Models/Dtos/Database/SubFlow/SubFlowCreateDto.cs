using System.Collections.ObjectModel;

namespace Core.Models.Dtos
{
    public class SubFlowCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public int OrderNumber { get; set; }

        public virtual IEnumerable<FlowStepCreateDto> FlowSteps { get; set; } = new Collection<FlowStepCreateDto>();
        public virtual IEnumerable<FlowSearchAreaCreateDto> FlowSearchAreas { get; set; } = new Collection<FlowSearchAreaCreateDto>();
    }
}
