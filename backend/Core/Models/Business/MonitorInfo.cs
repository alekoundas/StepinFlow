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

        /// <summary>
        /// Bounds in real device pixels. The process is Per-Monitor-V2 aware, so Win32 reports
        /// physical pixels everywhere and this is the only screen space the app uses.
        /// </summary>
        public Rectangle Bounds { get; set; }

        /// <summary>Effective DPI. 96 = 100% scale.</summary>
        public int Dpi { get; set; } = 96;

        public double ScaleFactor => Dpi / 96d;
    }
}
