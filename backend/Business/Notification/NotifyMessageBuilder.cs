using System.Globalization;

using Core.Enums;
using Core.Helpers;
using Core.Models.Database;

namespace Business.Notification
{
    /// <summary>
    /// Turns a Notify step, and the step whose result it includes, into the text that gets posted.
    ///
    /// One shape for every step type, with only the result line varying:
    ///
    ///     **Flow - Step - Type**
    ///     what came back from the step
    ///     ```
    ///     what it printed, for a command
    ///     ```
    ///
    ///     the user's own line
    ///
    /// The result is read from what this execution recorded, not from how the step is set up.
    /// </summary>
    public static class NotifyMessageBuilder
    {
        private const string ELLIPSIS = "\n(truncated)";
        private const int FENCE_OVERHEAD = 10;
        private const int MIN_OUTPUT_ROOM = 40;

        public static string Build(string flowName, string notifyMessage, FlowStep? reportedStep, ExecutionStep? reportedResult)
        {
            string header = $"**{Header(flowName, reportedStep)}**";
            string message = notifyMessage.Trim();

            if (reportedStep == null)
                return Clamp(message.Length == 0 ? header : $"{header}\n{message}");

            string tail = message.Length == 0 ? string.Empty : $"\n\n{message}";

            // The result is what gets cut, never the header or the user's line: a noisy result must not eat the sentence somebody wrote.
            int room = DiscordNotifier.MaxContentLength - header.Length - tail.Length - 1;
            string result = Fit(Outcome(reportedStep, reportedResult), Output(reportedStep, reportedResult), room);

            return Clamp($"{header}\n{result}{tail}");
        }

        /// <summary>"Search Image" reads better in an alert than SEARCH_IMAGE.</summary>
        public static string DisplayType(FlowStepTypeEnum type)
        {
            return string.Join(' ', type.ToString()
                .Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => char.ToUpperInvariant(x[0]) + x[1..].ToLowerInvariant()));
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string Header(string flowName, FlowStep? reportedStep)
        {
            return reportedStep == null
                   ? flowName
                   : $"{flowName} — {reportedStep.Name} — {DisplayType(reportedStep.FlowStepType)}";
        }

        private static string Outcome(FlowStep step, ExecutionStep? result)
        {
            if (result == null)
                return "No result yet: it has not been executed.";

            bool passed = result.Outcome == StepOutcomeEnum.SUCCESS;

            switch (step.FlowStepType)
            {
                case FlowStepTypeEnum.SEARCH_IMAGE:
                    return ImageOutcome(step, result, passed);

                case FlowStepTypeEnum.SEARCH_TEXT:
                    return TextOutcome(step, result, passed);

                case FlowStepTypeEnum.CHECK_VALUE:
                    if (passed)
                        return $"\"{result.Value}\" satisfies {ConditionEvaluatorHelper.Describe(step)}.";

                    return Said(result, "Failed");

                case FlowStepTypeEnum.SYSTEM_COMMAND:
                    if (passed && result.ExitCode is int exitCode)
                        return string.Create(CultureInfo.InvariantCulture, $"Exit code {exitCode}.");

                    return Said(result, passed ? "Succeeded" : "Failed");

                default:
                    return Said(result, passed ? "Succeeded" : "Failed");
            }
        }

        // Each template has its own accuracy, so a score means nothing until it is read against the bar of the one that scored it.
        private static string ImageOutcome(FlowStep step, ExecutionStep result, bool passed)
        {
            bool waitsUntilGone = step.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND;
            if (passed && waitsUntilGone)
                return "No longer on screen.";

            FlowStepTemplate? closest = step.FlowStepTemplates.FirstOrDefault(x => x.Id == result.BestTemplateId);
            if (closest == null || result.BestScore == null)
                return Said(result, passed ? "Found" : "Not found");

            string score = $"\"{closest.FlowTemplate.Name}\" at {Score(result.BestScore.Value)} accuracy, {Score(closest.Accuracy)} needed";

            if (waitsUntilGone)
                return $"Still on screen: {score}.";

            if (!passed)
                return $"Not found. Closest was {score}.";

            if (result.MatchCount > 1)
                return string.Create(CultureInfo.InvariantCulture, $"Found {result.MatchCount} times. Best was {score}.");

            return $"Found {score}.";
        }

        private static string TextOutcome(FlowStep step, ExecutionStep result, bool passed)
        {
            if (step.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND || result.Value == null)
                return Said(result, passed ? "Found" : "Not found");

            if (passed)
                return $"Text found: \"{result.Value}\"";

            return Said(result, "Text not found");
        }

        // What a command printed, which is too long and too many lines for the sentence.
        private static string Output(FlowStep step, ExecutionStep? result)
        {
            if (step.FlowStepType != FlowStepTypeEnum.SYSTEM_COMMAND || result == null || string.IsNullOrWhiteSpace(result.Value))
                return string.Empty;

            return result.Value.Trim();
        }

        // The engine's own sentence about why, after a word saying which way it went.
        private static string Said(ExecutionStep result, string outcome)
        {
            if (string.IsNullOrWhiteSpace(result.Message))
                return $"{outcome}.";

            return $"{outcome}: {result.Message}";
        }

        private static string Fit(string line, string output, int room)
        {
            if (line.Length >= room)
                return Cut(line, room);

            int outputRoom = room - line.Length - FENCE_OVERHEAD;
            if (output.Length == 0 || outputRoom < MIN_OUTPUT_ROOM)
                return line;

            return $"{line}\n```\n{Cut(output, outputRoom)}\n```";
        }

        private static string Cut(string text, int room)
        {
            if (text.Length <= room)
                return text;

            return text[..Math.Max(0, room - ELLIPSIS.Length)] + ELLIPSIS;
        }

        private static string Score(float value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string Clamp(string content)
        {
            return content.Length <= DiscordNotifier.MaxContentLength
                   ? content
                   : content[..DiscordNotifier.MaxContentLength];
        }
    }
}
