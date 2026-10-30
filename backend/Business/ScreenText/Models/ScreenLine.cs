using System.Drawing;

namespace Business.ScreenText.Models
{
    /// <summary>Words OCR read close together on one line, read as one thing: a label, a heading, a sentence.</summary>
    public sealed class ScreenLine
    {
        public string Text { get; init; } = string.Empty;

        /// <summary>In the pixels of the screenshot it was read from.</summary>
        public Rectangle Bounds { get; init; }
    }
}
