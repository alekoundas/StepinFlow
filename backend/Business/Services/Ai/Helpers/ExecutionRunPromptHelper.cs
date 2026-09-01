using System.Text;
using Core.Enums;
using Core.Models.Dtos;

namespace Business.Services.Ai.Helpers
{
    /// <summary>
    /// Turns an execution run into the text a model reads.
    ///
    /// Ordered by sequence and indented by depth it reads like a stack trace, which is the shape a language
    /// model handles best - and it is exactly why both columns are stored.
    ///
    /// Two things it has to get right:
    /// 1) a long run will not fit, so it keeps the failure, everything the failure ran inside, and the steps just before it. 
    /// 2) text read off the screen is whatever was on the screen, so it only goes out when the user has said it may.
    /// </summary>
    public static class ExecutionRunPromptHelper
    {
        private const int _maxSteps = 60;// Roughly what fits comfortably alongside a system prompt on a small local model.
        private const int _stepsBeforeFailure = 30; //How much run-up to keep before the failure when a run has to be cut down.
        private const int _stepsAfterFailure = 10; //And how much after it. A run does not always stop at its last failure .

        private const string _redacted = "(hidden)";

        public static string Format(ExecutionDto execution, bool includeScreenValues)
        {
            List<ExecutionStepDto> steps = execution.ExecutionSteps.OrderBy(x => x.Sequence).ToList();
            List<ExecutionStepDto> kept = RemoveExcessSteps(steps, execution.ErrorFlowStepId, out int omitted);

            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"Run #{execution.Id} of flow {execution.FlowId}");
            builder.AppendLine($"Result: {execution.Status}");
            builder.AppendLine($"Steps that ran: {execution.StepCount}");

            if (!string.IsNullOrWhiteSpace(execution.ErrorMessage))
                builder.AppendLine($"The run ended with: {execution.ErrorMessage}");

            builder.AppendLine();

            if (omitted > 0)
                builder.AppendLine($"({omitted} steps away from the failure left out to keep this short.)");

            int previousSequence = -1;

            foreach (ExecutionStepDto step in kept)
            {
                // A gap means steps were dropped between these two, and the model should not read
                // them as having run one after the other.
                if (previousSequence >= 0 && step.Sequence != previousSequence + 1)
                    builder.AppendLine("   ...");

                builder.AppendLine(ExecutionStepToText(step, execution.ErrorFlowStepId, includeScreenValues));
                previousSequence = step.Sequence;
            }

            return builder.ToString();
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string ExecutionStepToText(ExecutionStepDto step, int? errorFlowStepId, bool includeScreenValues)
        {
            bool isFatal = errorFlowStepId != null && step.FlowStepId == errorFlowStepId;
            bool isFailure = step.Outcome == StepOutcomeEnum.FAILURE;

            StringBuilder line = new StringBuilder();

            line.Append($"[{step.Sequence}] ");
            line.Append(new string(' ', step.Depth * 2));
            line.Append(step.Name);
            line.Append($"  {step.FlowStepType}");
            line.Append($"  {step.Outcome}");
            line.Append($"  {step.DurationMilliseconds}ms");

            if (step.MatchCount != null)
                line.Append($"  match {(step.MatchIndex ?? 0) + 1} of {step.MatchCount}");

            if (step.LoopPass != null)
                line.Append($"  loop pass {step.LoopPass + 1}");

            if (step.ExitCode != null)
                line.Append($"  exit code {step.ExitCode}");

            if (!string.IsNullOrWhiteSpace(step.Value))
                line.Append($"  read: \"{(includeScreenValues ? step.Value : _redacted)}\"");

            // The distinction the whole answer turns on. A failure with a Failure branch under it
            // is the flow working; only this one stopped the run.
            if (isFatal)
                line.Append("   <-- THIS ENDED THE RUN");
            else if (isFailure)
                line.Append("   (handled - a Failure branch took over)");

            if (!string.IsNullOrWhiteSpace(step.Message))
                line.Append($"{Environment.NewLine}      {Redact(step.Message, step.Value, includeScreenValues)}");

            if (!string.IsNullOrWhiteSpace(step.Error))
                line.Append($"{Environment.NewLine}      stderr: {Redact(step.Error, step.Value, includeScreenValues)}");

            return line.ToString();
        }

      
        private static string Redact(string text, string? value, bool includeScreenValues)
        {
            if (includeScreenValues || string.IsNullOrWhiteSpace(value))
                return text;

            return text.Replace(value, _redacted, StringComparison.Ordinal);
        }

        /// <summary>
        /// Cuts a long run down to what explains the failure: the step that ended it, every step it
        /// ran inside, and the run-up. Walking ParentSequence up is the same idea as the rule about
        /// which results a step is allowed to read - the chain of parents is the causal context.
        /// </summary>
        private static List<ExecutionStepDto> RemoveExcessSteps(List<ExecutionStepDto> steps, int? errorFlowStepId, out int omitted)
        {
            omitted = 0;

            if (steps.Count <= _maxSteps)
                return steps;

            ExecutionStepDto? fatal = steps.LastOrDefault(x => errorFlowStepId != null && x.FlowStepId == errorFlowStepId)
                ?? steps.LastOrDefault(x => x.Outcome == StepOutcomeEnum.FAILURE)
                ?? steps.LastOrDefault();

            if (fatal == null)
                return steps;

            HashSet<int> keep = new HashSet<int>();

            // Everything the failure ran inside.
            Dictionary<int, ExecutionStepDto> bySequence = steps.ToDictionary(x => x.Sequence);
            ExecutionStepDto? walk = fatal;

            while (walk != null)
            {
                keep.Add(walk.Sequence);

                if (walk.ParentSequence == null)
                    break;

                bySequence.TryGetValue(walk.ParentSequence.Value, out walk);
            }

            // The run-up, and a little of whatever came after in case the run carried on. Bounded
            // at both ends: a failure the flow handled can sit thousands of steps from the end, and
            // keeping everything past it is not a trim at all.
            foreach (ExecutionStepDto step in steps)
            {
                if (step.Sequence > fatal.Sequence - _stepsBeforeFailure
                    && step.Sequence < fatal.Sequence + _stepsAfterFailure)
                    keep.Add(step.Sequence);
            }

            List<ExecutionStepDto> kept = steps.Where(x => keep.Contains(x.Sequence)).ToList();
            omitted = steps.Count - kept.Count;

            return kept;
        }
    }
}
