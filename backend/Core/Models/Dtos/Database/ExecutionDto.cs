using System.Collections.ObjectModel;

using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>One run of a flow. ExecutionSteps is filled only when a single run is opened.</summary>
    public class ExecutionDto
    {
        public int Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? CompletedAt { get; set; }

        public ExecutionStatusEnum Status { get; set; }
        public ExecutionHistoryLevelEnum HistoryLevel { get; set; }
        public int StepCount { get; set; }

        public int? ErrorFlowStepId { get; set; } //The failure that ended the run - a step that failed into a Failure branch is not this
        public string ErrorMessage { get; set; } = string.Empty;

        public string FlowStructureHash { get; set; } = string.Empty; //The shape of the flow at the time - a run stops being replayable once this stops matching

        public int FlowId { get; set; }

        public IEnumerable<ExecutionStepDto> ExecutionSteps { get; set; } = new Collection<ExecutionStepDto>();
    }
}
