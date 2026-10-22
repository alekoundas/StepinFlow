namespace Core.Models.Dtos
{
    /// <summary>
    /// A rectangle in physical (real device) pixels on the virtual desktop.
    /// </summary>
    public class ScreenRectangleDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
