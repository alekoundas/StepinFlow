using Core.Models.Business;
using Core.Ports;
using Platform.Windows.Native;
using System.Drawing;

namespace Platform.Windows.Screen
{
    /// <summary>
    /// The two pieces of monitor geometry anything above the port needs.
    ///
    /// Thin over <see cref="ScreenMetrics"/>, which stays static because the rest of it cannot go
    /// through a container: enabling DPI awareness runs once at startup before one exists, and
    /// FindHMonitorById hands back a raw monitor handle that must not cross the port.
    /// </summary>
    public sealed class ScreenService : IScreenService
    {
        public IReadOnlyList<MonitorInfo> GetAllMonitors()
        {
            return ScreenMetrics.GetAllMonitors();
        }

        public Rectangle GetVirtualScreenBounds()
        {
            return ScreenMetrics.GetVirtualScreenBounds();
        }
    }
}
