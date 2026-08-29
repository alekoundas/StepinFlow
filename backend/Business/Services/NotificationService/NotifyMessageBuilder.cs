using System.Text;

using Core.Enums;
using Core.Helpers;
using Core.Models.Database;

namespace Business.Services.NotificationService
{
    /// <summary>
    /// Turns a Notify step, and the step whose failure it reports, into the text that gets posted.
    ///
    /// One shape for every step type, with only the fenced block varying:
    ///
    ///     **Flow - Step - Type**
    ///     the user's own line
    ///
    ///     Step failed.
    ///     ```
    ///     what the step was actually trying to do
    ///     ```
    ///
    /// ExecutionStep.Message and Value
    /// </summary>
    public static class NotifyMessageBuilder
    {
        public const int MaxAttachments = 4;

        private const string Ellipsis = "\n(truncated)";
        private const int FenceOverhead = 10;
        private const int MinDetailRoom = 40;

        public static string Build(string flowName, FlowStep notifyStep, FlowStep? failedStep, IReadOnlyList<string> templateNames)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("**").Append(Header(flowName, failedStep)).Append("**");

            if (!string.IsNullOrWhiteSpace(notifyStep.NotifyMessage))
                builder.Append('\n').Append(notifyStep.NotifyMessage.Trim());

            if (failedStep == null)
                return Clamp(builder.ToString());

            builder.Append("\n\nStep failed.");

            string detail = Detail(failedStep, templateNames);

            // No dangling label when there is nothing to put under it, which is every type except image search until the engine starts recording results.
            if (string.IsNullOrWhiteSpace(detail))
                return Clamp(builder.ToString());

            // The detail is what gets cut, never the header or the user's line: a noisy failure must not eat the sentence somebody wrote.
            string head = builder.ToString();
            int room = DiscordNotifier.MaxContentLength - head.Length - FenceOverhead;

            if (room <= MinDetailRoom)
                return Clamp(head);

            if (detail.Length > room)
                detail = detail[..(room - Ellipsis.Length)] + Ellipsis;

            return head + "\n```\n" + detail + "\n```";
        }

        /// <summary>"Image Search" reads better in an alert than IMAGE_SEARCH.</summary>
        public static string DisplayType(FlowStepTypeEnum type)
        {
            return string.Join(' ', type.ToString()
                .Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => char.ToUpperInvariant(x[0]) + x[1..].ToLowerInvariant()));
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string Header(string flowName, FlowStep? failedStep)
        {
            return failedStep == null
                   ? flowName
                   : $"{flowName} — {failedStep.Name} — {DisplayType(failedStep.FlowStepType)}";
        }

        private static string Detail(FlowStep failedStep, IReadOnlyList<string> templateNames)
        {
            return failedStep.FlowStepType switch
            {
                FlowStepTypeEnum.IMAGE_SEARCH => ImageSearchDetail(failedStep, templateNames),
                FlowStepTypeEnum.READ_TEXT => $"read the screen and the result did not satisfy: {ConditionEvaluator.Describe(failedStep)}",
                FlowStepTypeEnum.CHECK_VALUE => $"the value did not satisfy: {ConditionEvaluator.Describe(failedStep)}",
                FlowStepTypeEnum.SYSTEM_COMMAND => $"the command did not exit with {failedStep.SuccessExitCodes}",

                FlowStepTypeEnum.WINDOW_FOCUS => "the window could not be found",
                FlowStepTypeEnum.WINDOW_RESIZE => "the window could not be found",
                FlowStepTypeEnum.WINDOW_RELOCATE => "the window could not be found",

                _ => string.Empty,
            };
        }

        private static string ImageSearchDetail(FlowStep failedStep, IReadOnlyList<string> templateNames)
        {
            if (templateNames.Count == 0)
                return "the step has no templates to look for";

            StringBuilder builder = new StringBuilder();

            builder.Append("none of these templates matched: ")
                   .Append(string.Join(", ", templateNames.Take(MaxAttachments)));

            if (templateNames.Count > MaxAttachments)
                builder.Append('\n').Append($"{templateNames.Count - MaxAttachments} more templates not shown");

            builder.Append('\n').Append($"search mode: {failedStep.SearchMode}, accuracy: {failedStep.Accuracy}");

            return builder.ToString();
        }

        private static string Clamp(string content)
        {
            return content.Length <= DiscordNotifier.MaxContentLength
                   ? content
                   : content[..DiscordNotifier.MaxContentLength];
        }
    }
}
