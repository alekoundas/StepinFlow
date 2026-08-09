namespace Core.Models.Dtos
{
    /// <summary>
    /// One monitor's frozen frame plus its bounds in physical pixels. Electron converts to DIPs
    /// with screen.screenToDipRect when it places the overlay window.
    /// </summary>
    public class ScreenshotMonitorResponseDto
    {
        public byte[] Screenshot { get; set; } = [];

        public string DeviceId { get; set; } = string.Empty;

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public int Dpi { get; set; }
    }
}
