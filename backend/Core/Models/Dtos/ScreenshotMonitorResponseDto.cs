namespace Core.Models.Dtos
{
    public class ScreenshotMonitorResponseDto
    {
        public byte[] Screenshot { get; set; } = [];

        public int LogicalX { get; set; }
        public int LogicalY { get; set; }
        public int PhysicalWidth { get; set; }
        public int PhysicalHeight { get; set; }
    }
}
