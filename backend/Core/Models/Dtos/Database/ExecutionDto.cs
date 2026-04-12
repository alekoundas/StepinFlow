using Core.Models.Database;
using System.Collections.ObjectModel;

namespace Core.Models.Dtos
{
    public class ExecutionDto
    {
        public int Id { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "Running"; // Running, Paused, Completed, Failed

        public string? CurrentStepPath { get; set; } // e.g. "1.2.3" for deep nesting
        public int CheckpointStepCount { get; set; } // for resume ? see if needed

        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;


        public IEnumerable<ExecutionStepDto> ExecutionSteps { get; set; } = new Collection<ExecutionStepDto>();// only in memory during run
    }
}
