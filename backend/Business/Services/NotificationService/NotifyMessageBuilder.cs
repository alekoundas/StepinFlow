using System.Text;

using Core.Enums;
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
    /// The user's line sits above the detail because it is the only human sentence in there, and
    /// the first line is what gets scanned in a busy channel. The detail is fenced because OCR
    /// output and stderr are full of underscores and asterisks that Discord would otherwise read
    /// as markdown.
    ///
    /// Image search is the only type that can say anything substantial today, and it manages that
    /// from saved configuration alone. The rest describe what was attempted rather than what came
    /// back, because nothing records a step result yet - ExecutionStep.ResultJson is where that
    /// goes once the engine exists.
    /// </summary>
    public static class NotifyMessageBuilder
    {
        /// <summary>Discord takes ten files; four is plenty to recognise what was being looked for.</summary>
        public const int MaxAttachments = 4;

        private const string Ellipsis = "\n(truncated)";

        /// <summary>The two fence lines the detail block is wrapped in.</summary>
        private const int FenceOverhead = 10;

        /// <summary>Below this there is no room worth truncating into, so the block is dropped.</summary>
        private const int MinDetailRoom = 40;

        public static string Build(
            string flowName,
            FlowStep notifyStep,
            FlowStep? failedStep,
            IReadOnlyList<string> templateNames)
        {
            StringBuilder builder = new();

            builder.Append("**").Append(Header(flowName, failedStep)).Append("**");

            if (!string.IsNullOrWhiteSpace(notifyStep.NotifyMessage))
                builder.Append('\n').Append(notifyStep.NotifyMessage.Trim());

            if (failedStep == null)
                return Clamp(builder.ToString());

            builder.Append("\n\nStep failed.");

            string detail = Detail(failedStep, templateNames);

            // No dangling label when there is nothing to put under it, which is every type except
            // image search until the engine starts recording results.
            if (string.IsNullOrWhiteSpace(detail))
                return Clamp(builder.ToString());

            // The detail is what gets cut, never the header or the user's line: a noisy failure
            // must not eat the sentence somebody wrote.
            string head = builder.ToString();
            int room = DiscordNotifier.MaxContentLength - head.Length - FenceOverhead;

            if (room <= MinDetailRoom)
                return Clamp(head);

            if (detail.Length > room)
                detail = detail[..(room - Ellipsis.Length)] + Ellipsis;

            return head + "\n```\n" + detail + "\n```";
        }

        /// <summary>"Image Search" reads better in an alert than IMAGE_SEARCH.</summary>
        public static string DisplayType(FlowStepTypeEnum type) =>
            string.Join(' ', type.ToString()
                .Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => char.ToUpperInvariant(x[0]) + x[1..].ToLowerInvariant()));


        // ================================================================
        // Private methods
        // ================================================================

        private static string Header(string flowName, FlowStep? failedStep) =>
            failedStep == null
                ? flowName
                : $"{flowName} — {failedStep.Name} — {DisplayType(failedStep.FlowStepType)}";

        private static string Detail(FlowStep failedStep, IReadOnlyList<string> templateNames) =>
            failedStep.FlowStepType switch
            {
                FlowStepTypeEnum.IMAGE_SEARCH => ImageSearchDetail(failedStep, templateNames),
                FlowStepTypeEnum.READ_TEXT => $"read the screen and the result did not satisfy: {Condition(failedStep)}",
                FlowStepTypeEnum.CHECK_VALUE => $"the value did not satisfy: {Condition(failedStep)}",
                FlowStepTypeEnum.SYSTEM_COMMAND => $"the command did not exit with {failedStep.SuccessExitCodes}",

                FlowStepTypeEnum.WINDOW_FOCUS or
                FlowStepTypeEnum.WINDOW_RESIZE or
                FlowStepTypeEnum.WINDOW_RELOCATE => "the window could not be found",

                _ => string.Empty,
            };

        private static string ImageSearchDetail(FlowStep failedStep, IReadOnlyList<string> templateNames)
        {
            if (templateNames.Count == 0)
                return "the step has no templates to look for";

            StringBuilder builder = new();

            builder.Append("none of these templates matched: ")
                   .Append(string.Join(", ", templateNames.Take(MaxAttachments)));

            if (templateNames.Count > MaxAttachments)
                builder.Append('\n').Append($"{templateNames.Count - MaxAttachments} more templates not shown");

            builder.Append('\n').Append($"search mode: {failedStep.SearchMode}, accuracy: {failedStep.Accuracy}");

            return builder.ToString();
        }

        private static string Condition(FlowStep failedStep)
        {
            string end = string.IsNullOrWhiteSpace(failedStep.ConditionTextEnd)
                ? string.Empty
                : $" .. {failedStep.ConditionTextEnd}";

            return $"{failedStep.ConditionType} {failedStep.ConditionText}{end}".Trim();
        }

        private static string Clamp(string content) =>
            content.Length <= DiscordNotifier.MaxContentLength
                ? content
                : content[..DiscordNotifier.MaxContentLength];
    }
}
