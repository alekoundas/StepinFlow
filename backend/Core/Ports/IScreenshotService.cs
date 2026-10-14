using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using System.Drawing;

namespace Core.Ports
{
    // Dependency Inversion Principle(DIP)

    /// <summary>
    /// Takes Screenshots.
    /// </summary>
    public interface IScreenshotService
    {
        byte[] Capture(Rectangle rect, ScreenshotFormatEnum screenshotFormat, int jpegQuality);
        RawImage CaptureRaw(Rectangle rect);

        byte[] Encode(RawImage image, ScreenshotFormatEnum screenshotFormat, int jpegQuality);

        /// <summary>A PNG or JPEG back to pixels, laid out as a capture is.</summary>
        RawImage Decode(byte[] encoded);
        byte[] CaptureVirtualScreen(ScreenshotFormatEnum screenshotFormat, int jpegQuality);

        /// <summary>Bounds come from IAreaPointResolver, so nesting is already applied.</summary>
        byte[] CaptureResolvedArea(FlowArea area, Rectangle bounds, ScreenshotFormatEnum screenshotFormat, int jpegQuality);

        byte[] CaptureAppWindow(string appWindowName, ScreenshotFormatEnum screenshotFormat, int jpegQuality);
        byte[] CaptureMonitor(string deviceName, ScreenshotFormatEnum screenshotFormat, int jpegQuality);
    }
}
