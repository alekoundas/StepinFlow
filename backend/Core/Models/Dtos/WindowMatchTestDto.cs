using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>A matcher straight off the form, before anything is saved.</summary>
    public class WindowMatchTestRequestDto
    {
        public string ProcessName { get; set; } = string.Empty;
        public string TitlePattern { get; set; } = string.Empty;
        public TitleMatchModeEnum TitleMatchMode { get; set; }

        /// <summary>
        /// So the reported bounds are the ones the area will actually resolve to. A test that
        /// measured differently from the thing being configured would be worse than none.
        /// </summary>
        public bool UseClientArea { get; set; } = true;
    }

    public class WindowMatchTestResultDto
    {
        /// <summary>
        /// Every window that matches, in z-order. The count is the point: with the first match
        /// winning, "3 match" is the only way to learn a pattern is too broad.
        /// </summary>
        public List<WindowMatchDto> Matches { get; set; } = new List<WindowMatchDto>();
    }

    public class WindowMatchDto
    {
        public string Title { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
