using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>One execution as the executions feed shows it, with the names it would otherwise look up.</summary>
    public class ExecutionFeedItemDto
    {
        public int Id { get; set; }
        public int FlowId { get; set; }
        public string FlowName { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; }
        public DateTime? CompletedAt { get; set; }

        public ExecutionStatusEnum Status { get; set; }
        public ExecutionHistoryLevelEnum HistoryLevel { get; set; }
        public int StepCount { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>The step that threw, when the execution errored. Null otherwise, or once that step is deleted.</summary>
        public string? ErrorStepName { get; set; }

        /// <summary>Where its screenshots are. Null when it kept none.</summary>
        public string? ScreenshotFolderPath { get; set; }

        public string? CommitSha { get; set; }
        public bool? HasUncommittedChanges { get; set; }
    }
}
