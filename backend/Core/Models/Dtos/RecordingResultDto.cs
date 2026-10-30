namespace Core.Models.Dtos
{
    /// <summary>A stopped recording once worked through: what the review opens on.</summary>
    public class RecordingResultDto
    {
        public List<RecordedActionDto> Actions { get; set; } = [];

        /// <summary>What each click and drag inside the main area hit. Empty with no main area, or when finding it failed.</summary>
        public List<ClickedElementDto> ClickedElements { get; set; } = [];

        /// <summary>The clicks that hit the same picture. Empty when none did, or grouping failed.</summary>
        public List<ClickGroupDto> ClickGroups { get; set; } = [];

        /// <summary>What changed on the screen between one press and the next. Empty with fewer than two presses, or when reading failed.</summary>
        public List<ScreenChangeDto> ScreenChanges { get; set; } = [];
    }
}
