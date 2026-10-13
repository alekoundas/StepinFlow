using System.Drawing;
using Core.Models.Business;

namespace Business.Recording.Models.Session
{
    /// <summary>
    /// Where the main area was at one moment, and which window it is. The window is None for a
    /// main area that is a monitor or a region, which sorts presses by its bounds instead.
    /// </summary>
    public sealed class RecordingTarget
    {
        public RecordingTarget(Rectangle bounds, int dpi, WindowHandle window, int processId)
        {
            Bounds = bounds;
            Dpi = dpi;
            Window = window;
            ProcessId = processId;
        }

        public Rectangle Bounds { get; }
        public int Dpi { get; }
        public WindowHandle Window { get; }
        public int ProcessId { get; }
    }
}
