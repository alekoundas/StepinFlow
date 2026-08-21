namespace Core.Models.Dtos
{
    /// <summary>Where the saved draft ended up, so the caller can navigate straight to it.</summary>
    public class FlowDraftResultDto
    {
        public int FlowId { get; set; }
        public int FirstFlowStepId { get; set; }
        public int CreatedCount { get; set; }
    }
}
