using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>A gesture the recording paused itself at, to ask what it was for.</summary>
    public class RecordingGestureAskDto
    {
        /// <summary>The press it was, which the answer is kept by.</summary>
        public int PressIndex { get; set; }

        public RecordingGestureEnum Gesture { get; set; }

        /// <summary>Whether what it pressed has been found, so the window asks for it again.</summary>
        public bool IsElementFound { get; set; }
    }
}
