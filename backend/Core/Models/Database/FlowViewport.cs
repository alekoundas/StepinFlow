namespace Core.Models.Database
{
    public class FlowViewport : BaseDbModel
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int OrderNumber { get; set; }

        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;
    }
}
