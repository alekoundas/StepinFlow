namespace Core.Models.Database
{
    public class ExecutionStep : BaseDbModel
    {
        public int FlowStepId { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed
                                                        //public string? ResultJson { get; set; } // screenshot base64, coords, OpenCV result, etc.

        public int? ResultLocationX { get; set; }
        public int? ResultLocationY { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int ExecutionId { get; set; }
        public Execution Execution { get; set; } = null!;
    }
}
