namespace Core.Models.Dtos
{
    /// <summary>The Check page's "Ask for changes": the flow, and what the tester wrote.</summary>
    public class RecordingDraftRevisionDto
    {
        public int FlowId { get; set; }
        public string Changes { get; set; } = string.Empty;
    }
}
