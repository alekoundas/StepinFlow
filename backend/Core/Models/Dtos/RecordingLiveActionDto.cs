using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>
    /// One action as the live list says it while recording. Typed text is counted rather than
    /// shown, so a password never appears on screen.
    /// </summary>
    public class RecordingLiveActionDto
    {
        public int Index { get; set; }
        public RecordedActionKindEnum Kind { get; set; }
        public string Line { get; set; } = string.Empty;

        /// <summary>Since the recording started.</summary>
        public int ElapsedMilliseconds { get; set; }
    }
}
