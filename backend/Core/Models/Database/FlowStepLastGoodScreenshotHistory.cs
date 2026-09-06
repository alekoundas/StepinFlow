namespace Core.Models.Database
{
    public class FlowStepLastGoodScreenshotHistory : BaseDbModel
    {
        public string FileName { get; set; } = string.Empty;
        public int ViewportWidth { get; set; }
        public int ViewportHeight { get; set; }

        public int ExecutionId { get; set; }

        public int FlowStepId { get; set; }
        public FlowStep FlowStep { get; set; } = null!;
    }
}
