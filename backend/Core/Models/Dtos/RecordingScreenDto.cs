namespace Core.Models.Dtos
{
    /// <summary>
    /// The whole main area at an action: the screenshot its press kept, or for a key or typed text,
    /// the one from the last click before it.
    /// </summary>
    public class RecordingScreenDto
    {
        /// <summary>The action whose press it is: the one asked about, or the last click before it.</summary>
        public int ActionIndex { get; set; }

        /// <summary>The main area as a PNG.</summary>
        public byte[] Image { get; set; } = [];

        // Where the main area was on screen, so a click's place inside it is its location minus these.
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Dpi { get; set; }
    }
}
