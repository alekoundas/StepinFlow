using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>
    /// What a click hit: the element's rectangle inside the main area, and the text on it. The
    /// rectangle is what the draft cuts a template from, and the text is what names the step.
    /// </summary>
    public class ClickedElementDto
    {
        /// <summary>The click or drag it belongs to, by its index among the recording's actions.</summary>
        public int ActionIndex { get; set; }

        // In the main area's pixels, from its top left.
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>Read by OCR. Empty when there is none, or none could be read.</summary>
        public string Text { get; set; } = string.Empty;

        public ElementFoundByEnum FoundBy { get; set; }

        /// <summary>Why it is a box around the click rather than what the model found. Null when the model found it.</summary>
        public string? Note { get; set; }

        /// <summary>The element as a PNG: what a template cut from it would be.</summary>
        public byte[] Image { get; set; } = [];
    }
}
