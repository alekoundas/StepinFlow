using Core.Enums;

namespace Core.Models.Dtos
{
    public class ScreenshotRequestDto
    {
        public ScreenshotFormatEnum FormatType { get; set; }
        public int JpegQuality { get; set; } = 100;


        // Capture by FlowSearchArea value
        public int? FlowSearchAreaId { get; set; }

        // Capture everything
        public bool CaptureVirtualScreen { get; set; }

        // Capture monitor by device name
        public string CaptureMonitor { get; set; } = string.Empty;

        // Capture application by name
        public string CaptureAppWindow { get; set; } = string.Empty;

        // Capture custom coords
        public int LocationX { get; set; }
        public int LocationY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

    }
}
