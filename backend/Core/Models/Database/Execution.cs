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


        public int FlowId { get; set; }
        public Flow Flow { get; set; } = null!;

        public IEnumerable<ExecutionStep> ExecutionSteps { get; set; } = new Collection<ExecutionStep>();
    }
}
