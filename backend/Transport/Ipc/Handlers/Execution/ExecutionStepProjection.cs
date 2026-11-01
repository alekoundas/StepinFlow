using System.Linq.Expressions;

using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers.Execution
{
    /// <summary>A saved step as the execution page reads it, whether the whole execution is asked for or a few of its steps.</summary>
    public static class ExecutionStepProjection
    {
        /// <summary>Projected rather than mapped: the page reads a flat list and indents it by Depth.</summary>
        public static Expression<Func<ExecutionStep, ExecutionStepDto>> Row { get; } = x => new ExecutionStepDto
        {
            Id = x.Id,
            Sequence = x.Sequence,
            ParentSequence = x.ParentSequence,
            Depth = x.Depth,
            LoopPass = x.LoopPass,
            Name = x.Name,
            FlowStepType = x.FlowStepType,
            Outcome = x.Outcome,
            StartedOn = x.StartedOn,
            DurationMilliseconds = x.DurationMilliseconds,
            ResultLocationX = x.ResultLocationX,
            ResultLocationY = x.ResultLocationY,
            BestScore = x.BestScore,
            BestTemplateId = x.BestTemplateId,
            MatchIndex = x.MatchIndex,
            MatchCount = x.MatchCount,
            Value = x.Value,
            Message = x.Message,
            ExitCode = x.ExitCode,
            Error = x.Error,
            Command = x.Command,
            ScreenshotFileName = x.ScreenshotFileName,
            ExecutionId = x.ExecutionId,
            FlowStepId = x.FlowStepId,
        };

        /// <summary>
        /// A score of infinity cannot be written as json, and one of them fails the whole response
        /// rather than the one field. Executions from before the matcher clamped its scores still
        /// hold them.
        /// </summary>
        public static void ClearUnwritableScores(IEnumerable<ExecutionStepDto> steps)
        {
            foreach (ExecutionStepDto step in steps)
                if (step.BestScore != null && !float.IsFinite(step.BestScore.Value))
                    step.BestScore = null;
        }
    }
}
