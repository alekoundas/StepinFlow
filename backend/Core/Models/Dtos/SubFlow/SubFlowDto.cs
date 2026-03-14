using System.Collections.ObjectModel;

namespace Core.Models.Dtos
{
    public class SubFlowDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public int OrderNumber { get; set; }

        public virtual IEnumerable<FlowStepDto> FlowSteps { get; set; } = new Collection<FlowStepDto>();
        public virtual IEnumerable<FlowSearchAreaDto> FlowSearchAreas { get; set; } = new Collection<FlowSearchAreaDto>();
    }
}
