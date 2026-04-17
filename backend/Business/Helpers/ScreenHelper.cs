using Core.Models.Business;
using Core.Models.Database;
using Microsoft.Win32;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Business.Helpers
{
    public static class ScreenHelper
    {
        //==================================================
        // P/Invoke Get system metrics
        //==================================================
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);


        //==================================================
        // P/Invoke declarations for monitors
        //==================================================
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFOEX
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }



        //==================================================
        // P/Invoke declarations for devices
        //==================================================
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

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

        private const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001;
        private const uint DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;







        //==================================================
        // Public methods
        //==================================================

        /// <summary>
        /// Get Virtual screen bounds
        /// </summary>
        public static Rectangle GetVirtualScreenBounds()
        {
            int x = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int y = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            return new Rectangle(x, y, width, height);
        }



        /// <summary>
        /// Returns all monitors with their bounds (in virtual screen coordinates)
        /// </summary>
        public static IReadOnlyList<MonitorInfo> GetAllMonitors()
        {
            List<MonitorInfo> result = new List<MonitorInfo>();

            uint i = 0;
            DISPLAY_DEVICE dd = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };

            while (EnumDisplayDevices(null, i++, ref dd, 0))
            {
                if ((dd.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == 0)
                    continue;

                byte[]? edid = GetEdidBytes(dd.DeviceID);
                if (edid == null) continue;

                string uniqueId = GetEdidUniqueId(edid);

                // Get current bounds for this device name
                Rectangle bounds = GetBoundsForDeviceName(dd.DeviceName);

                result.Add(new MonitorInfo
                {
                    UniqueId = uniqueId,
                    DeviceName = dd.DeviceName,           // e.g. \\.\DISPLAY1
                    FriendlyName = dd.DeviceString,
                    IsPrimary = (dd.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0,
                    Bounds = bounds
                });
            }

            return result;
        }

        public static IntPtr FindHMonitorById(string monitorUniqueId)
        {
            uint i = 0;
            DISPLAY_DEVICE dd = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };

            while (EnumDisplayDevices(null, i++, ref dd, 0))
            {
                if ((dd.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == 0)
                    continue;

                byte[]? edid = GetEdidBytes(dd.DeviceID);
                if (edid == null) continue;

                string uniqueId = GetEdidUniqueId(edid);
                if(uniqueId == monitorUniqueId)
                {
                    return GetHandleForDeviceName(uniqueId);
                }
            }

            return IntPtr.Zero;
        }



        // ================================================================
        // Private helpers
        // ================================================================
        private static byte[]? GetEdidBytes(string? pnpDeviceId)
        {
            if (string.IsNullOrEmpty(pnpDeviceId)) return null;

            string keyPath = $@"SYSTEM\CurrentControlSet\Enum\{pnpDeviceId}\Device Parameters";
            using var key = Registry.LocalMachine.OpenSubKey(keyPath);
            return key?.GetValue("EDID") as byte[];
        }

        private static string GetEdidUniqueId(byte[] edid)
        {
            if (edid.Length < 20) return "INVALID_EDID";

            // Manufacturer (3 letters)
            ushort id = (ushort)((edid[8] << 8) | edid[9]);
            char m1 = (char)('A' + ((id >> 10) & 0x1F) - 1);
            char m2 = (char)('A' + ((id >> 5) & 0x1F) - 1);
            char m3 = (char)('A' + (id & 0x1F) - 1);
            string manufacturer = $"{m1}{m2}{m3}";

            // Product code
            int product = edid[10] | (edid[11] << 8);

            // Serial number (4 bytes)
            uint serial = BitConverter.ToUInt32(edid, 12);

            return $"{manufacturer}-{product:X4}-{serial:X8}";
        }

        private static Rectangle GetBoundsForDeviceName(string deviceName)
        {
            Rectangle monitorBounds = Rectangle.Empty;
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (hMonitor, hdcMonitor, ref lprcMonitor, dwData) =>
            {
                MONITORINFOEX mi = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };
                if (GetMonitorInfo(hMonitor, ref mi))
                    if (mi.szDevice == deviceName)
                    {
                        monitorBounds = Rectangle.FromLTRB(mi.rcMonitor.Left, mi.rcMonitor.Top, mi.rcMonitor.Right, mi.rcMonitor.Bottom);
                        return false; // Stop loop
                    }

                return true;
            }, IntPtr.Zero);


            return monitorBounds ;
        }

        private static IntPtr GetHandleForDeviceName(string deviceName)
        {
            IntPtr result = IntPtr.Zero;
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (hMonitor, hdcMonitor, ref lprcMonitor, dwData) =>
            {
                MONITORINFOEX mi = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };
                if (GetMonitorInfo(hMonitor, ref mi))
                    if (mi.szDevice == deviceName)
                    {
                        result = hMonitor;
                        return false; // Stop loop
                    }

                return true;
            }, IntPtr.Zero);


            return result;
        }
    }
}
