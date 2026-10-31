namespace Core.Models.Dtos
{
    /// <summary>The Check page's "Ask for changes": the flow, and what the tester wrote.</summary>
    public class RecordingDraftRevisionDto
    {
        public int FlowId { get; set; }
        public string Changes { get; set; } = string.Empty;

        /// <summary>The proposed checks not yet kept or dropped, by name, so the answer still offers them.</summary>
        public List<string> Undecided { get; set; } = [];
    }
}
