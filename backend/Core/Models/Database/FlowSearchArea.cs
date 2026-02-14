using Core.Models.Database;

namespace Core.Models
{
    public class FlowSearchArea : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;

        public int LocationLeft { get; set; }
        public int LocationTop { get; set; }
        public int LocationToRight { get; set; }
        public int LocationToBottom { get; set; }

        public int FlowId { get; set; }
        public virtual Flow Flow { get; set; } = null!;
    }
}
