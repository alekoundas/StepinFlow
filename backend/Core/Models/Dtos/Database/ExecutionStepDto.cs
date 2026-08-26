using Core.Enums;

namespace Core.Models.Dtos
{
    public class ExecutionStepDto
    {
        public int Id { get; set; }

        // Where it sits in the run
        public int Sequence { get; set; }
        public int? ParentSequence { get; set; }
        public int Depth { get; set; }
        public int? LoopPass { get; set; }

        public string Name { get; set; } = string.Empty;
        public FlowStepTypeEnum FlowStepType { get; set; }
        public StepOutcomeEnum Outcome { get; set; }
        public DateTime StartedOn { get; set; }
        public int DurationMilliseconds { get; set; }

        public int? ResultLocationX { get; set; }
        public int? ResultLocationY { get; set; }
        public int? MatchIndex { get; set; }
        public int? MatchCount { get; set; }

        // What came back
        public string? Value { get; set; }
        public string? Message { get; set; }


        // SYSTEM_COMMAND
        public int? ExitCode { get; set; }
        public string? Error { get; set; }
        public string? Command { get; set; }

        public string? ResultImagePath { get; set; }

        public int ExecutionId { get; set; }
        public int? FlowStepId { get; set; }
    }
}
