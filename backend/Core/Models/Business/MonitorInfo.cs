using System.Drawing;

namespace Core.Models.Business
{
    public class MonitorInfo
    {
        public string DeviceId { get; set; } = string.Empty;
        public string AdapterName { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool IsVirtual { get; set; }
        public IntPtr HMonitor { get; set; }

        public Rectangle Bounds { get; set; }
    }
}
