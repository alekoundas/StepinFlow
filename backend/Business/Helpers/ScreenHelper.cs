using Core.Models.Business;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Business.Helpers
{
    /// <summary>
    /// Monitor geometry in real device pixels.
    ///
    /// <see cref="EnablePerMonitorDpiAwareness"/> must run before anything else touches a
    /// coordinate API. Without it Windows virtualizes every rect to 96 DPI and nothing lines up
    /// with the capture buffers or the low level input hook, both of which are always physical.
    /// </summary>
    public static class ScreenHelper
    {
        //==================================================
        // P/Invoke Process DPI awareness
        //==================================================
        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);


        //==================================================
        // P/Invoke Get all Monitors and get Monitor info
        //==================================================
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MONITORINFOEX
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);


        //==================================================
        // P/Invoke Monitor DPI
        //==================================================
        private const int MDT_EFFECTIVE_DPI = 0;

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);


        //==================================================
        // P/Invoke Get all Display Devices
        //==================================================
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        private const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001;
        private const uint DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;




        //==================================================
        // Public methods
        //==================================================

        /// <summary>
        /// Call once, first thing at startup, before any coordinate API.
        /// </summary>
        public static bool EnablePerMonitorDpiAwareness()
        {
            return SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        }

        /// <summary>
        /// Bounding box of every monitor, in real device pixels. This is the canvas size a
        /// stitched all-monitor screenshot has to be.
        /// </summary>
        public static Rectangle GetVirtualScreenBounds()
        {
            return UnionBounds(GetEveryMonitorInfo().Values.Select(x => x.Bounds));
        }

        public static IReadOnlyList<MonitorInfo> GetAllMonitors()
        {
            Dictionary<string, (IntPtr HMonitor, Rectangle Bounds, int Dpi)> everyMonitorInfos = GetEveryMonitorInfo();
            List<MonitorInfo> result = new List<MonitorInfo>();
            int index = 1; // for fallback display numbering

            DISPLAY_DEVICE adapter = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
            for (uint ai = 0; EnumDisplayDevices(null, ai, ref adapter, 0); ai++)
            {
                if ((adapter.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == 0)
                    continue;

                DISPLAY_DEVICE monitor = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
                for (uint i = 0; EnumDisplayDevices(adapter.DeviceName, i, ref monitor, 0); i++)
                {
                    bool isVirtual = !monitor.DeviceID.StartsWith("MONITOR\\", StringComparison.OrdinalIgnoreCase);

                    everyMonitorInfos.TryGetValue(adapter.DeviceName, out var monitorInfo);

                    string friendlyName = BuildFriendlyName(monitor, adapter, monitorInfo.Bounds, index, isVirtual);

                    result.Add(new MonitorInfo
                    {
                        DeviceId = adapter.DeviceName,
                        FriendlyName = friendlyName,
                        AdapterName = adapter.DeviceName,
                        IsPrimary = (adapter.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0,
                        IsVirtual = isVirtual,
                        Bounds = monitorInfo.Bounds,
                        Dpi = monitorInfo.Dpi == 0 ? 96 : monitorInfo.Dpi,
                        HMonitor = monitorInfo.HMonitor
                    });

                    index++;
                }
            }

            return result;
        }

        public static IntPtr FindHMonitorById(string deviceId)
        {
            GetEveryMonitorInfo().TryGetValue(deviceId, out var monitorInfo);
            return monitorInfo.HMonitor;
        }

        /// <summary>
        /// The monitor a rectangle sits on, or null when it spans several.
        /// </summary>
        public static MonitorInfo? FindMonitorContaining(Rectangle rect)
        {
            IReadOnlyList<MonitorInfo> monitors = GetAllMonitors();
            List<MonitorInfo> touched = monitors.Where(x => x.Bounds.IntersectsWith(rect)).ToList();

            return touched.Count == 1 ? touched[0] : null;
        }



        // ================================================================
        // Private helpers
        // ================================================================

        private static Dictionary<string, (IntPtr HMonitor, Rectangle Bounds, int Dpi)> GetEveryMonitorInfo()
        {
            Dictionary<string, (IntPtr HMonitor, Rectangle Bounds, int Dpi)> map =
                new Dictionary<string, (IntPtr, Rectangle, int)>(StringComparer.OrdinalIgnoreCase);

            MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFOEX monitorInfo = new MONITORINFOEX();
                monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>();
                if (GetMonitorInfo(hMonitor, ref monitorInfo))
                {
                    Rectangle bounds = Rectangle.FromLTRB(
                        monitorInfo.rcMonitor.Left,
                        monitorInfo.rcMonitor.Top,
                        monitorInfo.rcMonitor.Right,
                        monitorInfo.rcMonitor.Bottom);

                    map[monitorInfo.szDevice] = (hMonitor, bounds, GetDpi(hMonitor));
                }

                return true;
            };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

            return map;
        }

        private static int GetDpi(IntPtr hMonitor)
        {
            if (GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _) != 0)
                return 96;

            return (int)dpiX;
        }

        private static Rectangle UnionBounds(IEnumerable<Rectangle> rectangles)
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            bool any = false;

            foreach (Rectangle bounds in rectangles)
            {
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    continue;

                any = true;
                minX = Math.Min(minX, bounds.X);
                minY = Math.Min(minY, bounds.Y);
                maxX = Math.Max(maxX, bounds.Right);
                maxY = Math.Max(maxY, bounds.Bottom);
            }

            return any ? new Rectangle(minX, minY, maxX - minX, maxY - minY) : Rectangle.Empty;
        }

        private static string BuildFriendlyName(
            DISPLAY_DEVICE monitor,
            DISPLAY_DEVICE adapter,
            Rectangle bounds,
            int index,
            bool isVirtual)
        {
            string baseName = "";
            if (string.IsNullOrWhiteSpace(monitor.DeviceString)
                || monitor.DeviceString.Contains("Generic", StringComparison.OrdinalIgnoreCase)
                || monitor.DeviceString.Contains("PnP", StringComparison.OrdinalIgnoreCase))
            {
                baseName = isVirtual ? "Virtual Monitor" : $"Monitor {index}";
            }
            else
            {
                baseName = monitor.DeviceString;
            }

            string resolution = $" ({bounds.Width}×{bounds.Height})";
            string primary = (adapter.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0 ? " [Primary]" : string.Empty;

            return $"{baseName}{resolution}{primary}";
            // → e.g.  "Odyssey G9 (5120×1440) [Primary]"
            //         "Monitor 2 (1920×1080)"
        }
    }
}
