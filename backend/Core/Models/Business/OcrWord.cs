using System.Drawing;

namespace Core.Models.Business
{
    /// <summary>One word OCR read: what it says, where it is in the image, and which of the engine's lines it is on.</summary>
    public sealed class OcrWord
    {
        public string Text { get; init; } = string.Empty;

        /// <summary>In the pixels of the image read.</summary>
        public Rectangle Bounds { get; init; }

        /// <summary>The engine's line, counted from 0 in the order it read them.</summary>
        public int LineIndex { get; init; }
    }
}
