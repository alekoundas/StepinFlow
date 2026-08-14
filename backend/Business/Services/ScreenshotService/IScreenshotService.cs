using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using System.Drawing;

namespace Business.Services.ScreenshotService
{
    public interface IScreenshotService
    {
        byte[] Capture(Rectangle rect, ScreenshotFormatEnum screenshotFormat, int jpegQuality);

        /// <summary>Uncompressed BGRA for the matcher. No JPEG in the hot path.</summary>
        RawImage CaptureRaw(Rectangle rect);
        byte[] CaptureVirtualScreen(ScreenshotFormatEnum screenshotFormat, int jpegQuality);

        /// <summary>Bounds come from IFrameResolver, so nesting is already applied.</summary>
        byte[] CaptureResolvedArea(FlowArea area, Rectangle bounds, ScreenshotFormatEnum screenshotFormat, int jpegQuality);

        byte[] CaptureAppWindow(string appWindowName, ScreenshotFormatEnum screenshotFormat, int jpegQuality);
        byte[] CaptureMonitor(string deviceName, ScreenshotFormatEnum screenshotFormat, int jpegQuality);
    }
}
