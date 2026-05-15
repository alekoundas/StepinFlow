using Core.Models.Business;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Business.Helpers
{
    public static class ScreenHelper
    {
        //==================================================
        // P/Invoke Get system metrics (used for virtual screen)
        //==================================================
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);


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
        /// Get Virtual screen bounds
        /// </summary>
        //public static Rectangle GetVirtualScreenBounds()
        //{
        //    int x = GetSystemMetrics(SM_XVIRTUALSCREEN);
        //    int y = GetSystemMetrics(SM_YVIRTUALSCREEN);
        //    int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        //    int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        //    return new Rectangle(x, y, width, height);
        //}
        public static Rectangle GetVirtualScreenBounds()
        {
            Dictionary<string, (IntPtr HMonitor, Rectangle Bounds)> allMonitors = GetEveryMonitorInfo();

            if (allMonitors.Count == 0)
                return new Rectangle(0, 0, 0, 0);

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (var monitor in allMonitors.Values)
            {
                Rectangle bounds = monitor.Bounds;
                minX = Math.Min(minX, bounds.X);
                minY = Math.Min(minY, bounds.Y);
                maxX = Math.Max(maxX, bounds.Right);
                maxY = Math.Max(maxY, bounds.Bottom);
            }

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }




        /// <summary>
        /// Returns all monitors with their bounds 
        /// coordinates are in the Win32 virtual screen coordinate space since process is DPI-unaware
        /// </summary>
        public static IReadOnlyList<MonitorInfo> GetAllMonitors()
        {
            Dictionary<string, (IntPtr HMonitor, Rectangle Bounds)> everyMonitorInfos = GetEveryMonitorInfo();
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
                        //DeviceId = monitor.DeviceID,
                        DeviceId = adapter.DeviceName,
                        FriendlyName = friendlyName,
                        AdapterName = adapter.DeviceName,
                        IsPrimary = (adapter.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0,
                        IsVirtual = isVirtual,
                        Bounds = monitorInfo.Bounds,
                        HMonitor = monitorInfo.HMonitor
                    });

                    index++;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns pointer handle for a specific Monitor
        /// </summary>
        public static IntPtr FindHMonitorById(string deviceId)
        {
            Dictionary<string, (IntPtr HMonitor, Rectangle Bounds)> everyMonitorInfos = GetEveryMonitorInfo();
            everyMonitorInfos.TryGetValue(deviceId, out var monitorInfo);
            return monitorInfo.HMonitor;
        }



        // ================================================================
        // Private helpers
        // ================================================================

        private static Dictionary<string, (IntPtr HMonitor, Rectangle Bounds)> GetEveryMonitorInfo()
        {
            Dictionary<string, (IntPtr HMonitor, Rectangle Bounds)> map = new Dictionary<string, (IntPtr HMonitor, Rectangle Bounds)>(StringComparer.OrdinalIgnoreCase);

            MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFOEX monitorInfo = new MONITORINFOEX();
                monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>();
                if (GetMonitorInfo(hMonitor, ref monitorInfo))
                {
                    map[monitorInfo.szDevice] = (
                        hMonitor,
                        Rectangle.FromLTRB(monitorInfo.rcMonitor.Left, monitorInfo.rcMonitor.Top, monitorInfo.rcMonitor.Right, monitorInfo.rcMonitor.Bottom)
                    );
                }

                return true;
            };


            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

            return map;
        }

        private static string BuildFriendlyName(
            DISPLAY_DEVICE monitor,
            DISPLAY_DEVICE adapter,
            Rectangle bounds,
            int index,
            bool isVirtual)
        {

            // Base name: prefer DeviceString if not generic, then fallback
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

            // Append resolution + primary tag
            string resolution = $" ({bounds.Width}×{bounds.Height})";
            string primary = (adapter.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0 ? " [Primary]" : string.Empty;

            return $"{baseName}{resolution}{primary}";
            // → e.g.  "Odyssey G9 (5120×1440) [Primary]"
            //         "Monitor 2 (1920×1080)"
            //         "Virtual Monitor (1920×1080)"
        }
    }
}
