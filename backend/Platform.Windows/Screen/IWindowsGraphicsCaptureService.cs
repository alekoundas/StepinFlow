using Core.Enums;

namespace Platform.Windows.Screen
{
    public interface IWindowsGraphicsCaptureService
    {
        byte[]? CaptureMonitorRaw(IntPtr hMonitor, out int width, out int height);
        byte[]? CaptureWindowRaw(IntPtr hwnd, out int width, out int height);
    }
}
