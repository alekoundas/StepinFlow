namespace Core.Models.Dtos
{
    /// <summary>
    /// Where a recording goes in a flow that already existed: child number Index of a step, or of
    /// the flow's top when ParentFlowStepId is null. The editor's "Record steps here".
    /// </summary>
    public class RecordingDraftPositionDto
    {
        public int FlowId { get; set; }
        public int? ParentFlowStepId { get; set; }
        public int Index { get; set; }
    }
}
