using Core.Enums;
using Core.Models.Database;
using System.Drawing;

namespace Business.Services.ScreenshotService
{
    public interface IScreenshotService
    {
        byte[] Capture(Rectangle rect, ScreenshotFormatEnum screenshotFormat, int jpegQuality);
        byte[] CaptureVirtualScreen();
        byte[] CaptureSearchArea(FlowSearchArea area);
    }
}
