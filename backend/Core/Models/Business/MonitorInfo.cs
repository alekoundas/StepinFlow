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
        /// Bounds in the Win32 virtual screen space this (DPI-unaware) process sees.
        /// </summary>
        public Rectangle Bounds { get; set; }

        /// <summary>
        /// Bounds in real device pixels, taken from the display mode (DEVMODE).
        /// Unaffected by DPI virtualization, so this is the space screenshots
        /// of several monitors can be stitched in.
        /// </summary>
        public Rectangle PhysicalBounds { get; set; }
    }
}
