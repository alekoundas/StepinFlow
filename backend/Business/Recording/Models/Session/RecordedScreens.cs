using Core.Models.Dtos;

namespace Business.Recording.Models.Session
{
    /// <summary>What OCR read on a recording's screenshots: what changed from each to the next, and every line read.</summary>
    public sealed class RecordedScreens
    {
        public List<ScreenChangeDto> Changes { get; } = [];

        /// <summary>Each line once, in the order first read.</summary>
        public List<string> Lines { get; } = [];
    }
}
