using Core.Enums;
using Core.Models.Database;
using System.Drawing;

namespace Business.Services.ScreenshotService
{
    public interface IScreenshotService
    {
        byte[] Capture(Rectangle rect, ScreenshotFormatEnum screenshotFormat, int jpegQuality);
        byte[] CaptureVirtualScreen(ScreenshotFormatEnum screenshotFormat, int jpegQuality);
        byte[] CaptureSearchArea(FlowSearchArea area);

        byte[] CaptureAppWindow(string appWindowName, ScreenshotFormatEnum screenshotFormat, int jpegQuality);

        byte[] CaptureMonitor(string deviceName, ScreenshotFormatEnum screenshotFormat, int jpegQuality);
    }
}
