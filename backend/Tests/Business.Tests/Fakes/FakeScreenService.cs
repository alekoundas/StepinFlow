using System.Drawing;

using Core.Models.Business;
using Core.Ports;

namespace Business.Tests.Fakes
{
    public sealed class FakeScreenService : IScreenService
    {
        public List<MonitorInfo> Monitors { get; } = new List<MonitorInfo>();

        public FakeScreenService WithMonitor(string deviceId, Rectangle bounds, int dpi, bool isPrimary = false)
        {
            Monitors.Add(new MonitorInfo { DeviceId = deviceId, Bounds = bounds, Dpi = dpi, IsPrimary = isPrimary });
            return this;
        }

        public IReadOnlyList<MonitorInfo> GetAllMonitors()
        {
            return Monitors;
        }

        public Rectangle GetVirtualScreenBounds()
        {
            throw new NotImplementedException();
        }
    }
}
