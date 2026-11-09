using Core.Enums;
using Core.Models.Business;

namespace Business.Recording.Models.Session
{
    /// <summary>
    /// A gesture the recording paused itself at, until it is answered: the press, which gesture, and
    /// the window that had the keyboard, which gets it back once the question's window closes.
    /// </summary>
    public sealed class RecordingGestureAsk
    {
        public int PressIndex { get; init; }
        public RecordingGestureEnum Gesture { get; init; }
        public WindowHandle InFront { get; init; } = WindowHandle.None;
    }
}
