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

        /// <summary>What changed on the screen between one press and the next, and from the last to the screen the recording stopped on. Empty when reading failed.</summary>
        public List<ScreenChangeDto> ScreenChanges { get; set; } = [];

        /// <summary>Every line OCR read on the main area, each once, in the order first read. Screen text.</summary>
        public List<string> ScreenLines { get; set; } = [];

        /// <summary>What each press landed on, as its application told Windows. Only presses an application answered for.</summary>
        public List<PressedElementDto> PressedElements { get; set; } = [];

        /// <summary>What the local model saw at the presses worth a look. Filled only as the recording goes to a model.</summary>
        public List<ScreenDescriptionDto> ScreenDescriptions { get; set; } = [];
    }
}
