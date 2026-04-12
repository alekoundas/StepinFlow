namespace Core.Models.Dtos
{
    public class ExecutionStepDto
    {
        public int Id { get; set; }
        public int FlowStepId { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed
                                                        //public string? ResultJson { get; set; } // screenshot base64, coords, OpenCV result, etc.

        public int? ResultLocationX { get; set; }
        public int? ResultLocationY { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int ExecutionId { get; set; }
        public ExecutionDto Execution { get; set; } = null!;
    }
}
