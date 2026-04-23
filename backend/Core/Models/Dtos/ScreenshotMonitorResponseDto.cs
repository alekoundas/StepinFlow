namespace Core.Models.Dtos
{
    public class ScreenshotMonitorResponseDto
    {
        public byte[] Screenshot { get; set; } = [];

        public int LocationX { get; set; }
        public int LocationY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
