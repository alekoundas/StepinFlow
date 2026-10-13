using System.Drawing;

namespace Business.Recording.Models.Session
{
    /// <summary>
    /// What the session kept of the main area at one press: where it was, at what DPI, and all of
    /// it from just before. A click is stored relative to these, which is what lets a recording
    /// work on another machine.
    /// </summary>
    public sealed class RecordedPress
    {
        public int Index { get; init; }
        public Rectangle MainAreaBounds { get; init; }
        public int Dpi { get; init; }

        /// <summary>PNG of the whole main area.</summary>
        public byte[] Screenshot { get; init; } = [];
    }
}
