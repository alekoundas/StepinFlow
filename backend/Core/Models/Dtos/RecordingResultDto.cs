namespace Core.Models.Dtos
{
    /// <summary>A stopped recording once worked through: what the review opens on.</summary>
    public class RecordingResultDto
    {
        public List<RecordedActionDto> Actions { get; set; } = [];

        /// <summary>What each click and drag inside the main area hit. Empty with no main area, or when finding it failed.</summary>
        public List<ClickedElementDto> ClickedElements { get; set; } = [];
    }
}
