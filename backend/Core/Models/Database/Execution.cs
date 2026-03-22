using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models.Database
{
    public class Execution : BaseDbModel
    {
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "Running"; // Running, Paused, Completed, Failed

        public string? CurrentStepPath { get; set; } // e.g. "1.2.3" for deep nesting
        public int CheckpointStepCount { get; set; } // for resume ? see if needed

        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;


        //[NotMapped]
        public List<ExecutionStep> ExecutionSteps { get; set; } = new(); // only in memory during run
    }
}
