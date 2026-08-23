using System.Drawing;

namespace Core.Models.Business
{
    /// <summary>One window a matcher found, with enough to recognise it on screen.</summary>
    public class WindowMatch
    {
        public string Title { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public Rectangle Bounds { get; set; }
    }
}
