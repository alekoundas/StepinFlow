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

        /// <summary>
        /// Encodes pixels that were already captured. Lets a caller show the very frame it
        /// matched against: capturing a second time for display would put the boxes over
        /// whatever moved in between.
        /// </summary>
        byte[] Encode(RawImage image, ScreenshotFormatEnum screenshotFormat, int jpegQuality);
        byte[] CaptureVirtualScreen(ScreenshotFormatEnum screenshotFormat, int jpegQuality);

        /// <summary>Bounds come from IAreaPointResolver, so nesting is already applied.</summary>
        byte[] CaptureResolvedArea(FlowArea area, Rectangle bounds, ScreenshotFormatEnum screenshotFormat, int jpegQuality);

        byte[] CaptureAppWindow(string appWindowName, ScreenshotFormatEnum screenshotFormat, int jpegQuality);
        byte[] CaptureMonitor(string deviceName, ScreenshotFormatEnum screenshotFormat, int jpegQuality);
    }
}
