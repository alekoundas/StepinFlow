using System.Collections.ObjectModel;

namespace Core.Models.Dtos
{
    public class FlowSearchAreaDto
    {
        public int Id { get; set; }


        public string Name { get; set; } = string.Empty;

        public int LocationLeft { get; set; }
        public int LocationTop { get; set; }
        public int LocationToRight { get; set; }
        public int LocationToBottom { get; set; }

        // Flow
        public int FlowId { get; set; }
        public FlowDto Flow { get; set; } = null!;

        public IEnumerable<FlowStepDto> FlowSteps { get; set; } = new Collection<FlowStepDto>();
    }
}
