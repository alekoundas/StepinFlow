using Core.Models.Dtos;

namespace Business.Recording.Models.Session
{
    /// <summary>
    /// What OCR read on a recording's screenshots: what changed from each to the next, which page
    /// each press was on, the line under each click, and every line read.
    /// </summary>
    public sealed class RecordedScreens
    {
        public List<ScreenChangeDto> Changes { get; } = [];

        public List<ScreenPageDto> Pages { get; } = [];

        /// <summary>The line of words under each click, by its action. None for a click on no words.</summary>
        public Dictionary<int, string> LineTexts { get; } = [];

        /// <summary>Each line once, in the order first read.</summary>
        public List<string> Lines { get; } = [];
    }
}
