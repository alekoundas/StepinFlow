using Core.Enums;
using Core.Models.Database;

namespace Core.Models.Dtos
{
    /// <summary>What the runner reports as it walks. The only thing a page needs to follow a run.</summary>
    public class ExecutionEventDto
    {
        public ExecutionEventTypeEnum Type { get; set; }
        public int ExecutionId { get; set; }

        public int? FlowStepId { get; set; }
        public string Name { get; set; } = string.Empty;
        public FlowStepTypeEnum FlowStepType { get; set; }

        public int? Sequence { get; set; }
        public int? ParentSequence { get; set; }
        public int? Depth { get; set; }
        public int? LoopPass { get; set; }

        public StepOutcomeEnum? Outcome { get; set; }
        public int? DurationMilliseconds { get; set; }
        public int? ResultLocationX { get; set; }
        public int? ResultLocationY { get; set; }
        public int? MatchIndex { get; set; }
        public int? MatchCount { get; set; }
        public string? Value { get; set; }
        public string? Message { get; set; }

        public ExecutionStatusEnum? Status { get; set; }
        public string? ErrorMessage { get; set; }


        // ================================================================
        // Public methods
        // ================================================================

        public static ExecutionEventDto Started(int executionId, FlowStep step)
        {
            return Of(ExecutionEventTypeEnum.STEP_STARTED, executionId, step);
        }

        public static ExecutionEventDto Paused(int executionId, FlowStep step)
        {
            return Of(ExecutionEventTypeEnum.PAUSED, executionId, step);
        }

        /// <summary>Everything comes off the execution step, which is what gets written too.</summary>
        public static ExecutionEventDto Finished(int executionId, ExecutionStep executionStep)
        {
            return new ExecutionEventDto
            {
                Type = ExecutionEventTypeEnum.STEP_FINISHED,
                ExecutionId = executionId,
                FlowStepId = executionStep.FlowStepId,
                Name = executionStep.Name,
                FlowStepType = executionStep.FlowStepType,
                Sequence = executionStep.Sequence,
                ParentSequence = executionStep.ParentSequence,
                Depth = executionStep.Depth,
                LoopPass = executionStep.LoopPass,
                Outcome = executionStep.Outcome,
                DurationMilliseconds = executionStep.DurationMilliseconds,
                ResultLocationX = executionStep.ResultLocationX,
                ResultLocationY = executionStep.ResultLocationY,
                MatchIndex = executionStep.MatchIndex,
                MatchCount = executionStep.MatchCount,
                Value = executionStep.Value,
                Message = executionStep.Message,
            };
        }

        public static ExecutionEventDto Ended(int executionId, ExecutionStatusEnum status, string error)
        {
            return new ExecutionEventDto
            {
                Type = ExecutionEventTypeEnum.RUN_ENDED,
                ExecutionId = executionId,
                Status = status,
                ErrorMessage = error,
            };
        }

        private static ExecutionEventDto Of(ExecutionEventTypeEnum type, int executionId, FlowStep step)
        {
            return new ExecutionEventDto
            {
                Type = type,
                ExecutionId = executionId,
                FlowStepId = step.Id,
                Name = step.Name,
                FlowStepType = step.FlowStepType,
            };
        }
    }
}
