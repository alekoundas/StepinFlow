using System.Collections.ObjectModel;
using Core.Enums;

namespace Core.Models.Database
{
    public class Execution : BaseDbModel
    {
        public DateTime? CompletedAt { get; set; }

        public ExecutionStatusEnum Status { get; set; } = ExecutionStatusEnum.RUNNING;
        public ExecutionHistoryLevelEnum HistoryLevel { get; set; }


        public int StepCount { get; set; }
        public int CsvRowIndex { get; set; }
        public int ViewportWidth { get; set; }
        public int ViewportHeight { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
        public string? ScreenshotFolderName { get; set; }
        public string FlowStructureHash { get; set; } = string.Empty;

        /// <summary>Set only when Status is ERRORED: the step that threw, and what it said.</summary>
        public int? ErrorFlowStepId { get; set; }

        /// <summary>The repository's last commit as the execution started. Null when it had none or could not be read.</summary>
        public string? CommitSha { get; set; }

        /// <summary>Whether the flow, or a sub-flow it calls, had changes not committed yet. Null when the repository could not be read.</summary>
        public bool? HasUncommittedChanges { get; set; }

        /// <summary>The flow's script as it executed, kept only when the commit cannot say: changes not committed, or no commit.</summary>
        public string? Script { get; set; }


        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;

        public IEnumerable<ExecutionStep> ExecutionSteps { get; set; } = new Collection<ExecutionStep>();
    }
}
