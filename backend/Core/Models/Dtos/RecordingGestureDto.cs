using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>
    /// The gesture being asked about: the main area as it was just before the press, where it was
    /// pressed, and what it pressed once found.
    /// </summary>
    public class RecordingGestureDto
    {
        public int PressIndex { get; set; }
        public RecordingGestureEnum Gesture { get; set; }

        /// <summary>The main area's screenshot, PNG. Null when the press kept none.</summary>
        public byte[]? Image { get; set; }

        /// <summary>The main area's bounds on the screen, and its DPI.</summary>
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Dpi { get; set; }

        /// <summary>Where it was pressed, on the screen.</summary>
        public int LocationX { get; set; }
        public int LocationY { get; set; }

        /// <summary>What the press hit and the words read on it. Null until found, or for a Ctrl + right click, which is looked at for nothing.</summary>
        public ClickedElementDto? Element { get; set; }

        /// <summary>Whether looking for it has finished, found or not.</summary>
        public bool IsElementFound { get; set; }
    }
}
