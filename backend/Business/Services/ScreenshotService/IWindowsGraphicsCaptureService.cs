using Core.Enums;

namespace Business.Services.ScreenshotService
{
    public interface IWindowsGraphicsCaptureService
    {
        byte[]? CaptureMonitorRaw(IntPtr hMonitor, out int width, out int height);
        byte[]? CaptureWindowRaw(IntPtr hwnd, out int width, out int height);
    }
}
