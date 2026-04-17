using Core.Models.Database;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Business.Services.ScreenshotService
{
    public static class AppWindowHelper
    {

        //==================================================
        // P/Invoke See if window is visible
        //==================================================
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);


        //==================================================
        // P/Invoke Get all app windows
        //==================================================
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);


        //==================================================
        // P/Invoke Get window name (max 512 chars)
        //==================================================
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, [Out] char[] lpString, int nMaxCount);
        private static readonly char[] _titleBuffer = new char[512]; // reuse buffer to reduce allocations
        private static string GetAppWindowText(IntPtr hWnd)
        {
            int length = GetWindowText(hWnd, _titleBuffer, _titleBuffer.Length);
            return new string(_titleBuffer, 0, length);
        }


        //==================================================
        // P/Invoke Get app window size (RECT) 
        //==================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);



        public static IntPtr FindHwndByTitle(string windowTitle)
        {
            IntPtr foundHwnd = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                string windowText = GetAppWindowText(hWnd);
                if (windowText.Contains(windowTitle, StringComparison.OrdinalIgnoreCase))
                {
                    foundHwnd = hWnd;
                    return false; // stop enumeration
                }
                return true;
            }, IntPtr.Zero);


            return foundHwnd; ;
        }


        public static Rectangle GetApplicationWindowBounds(string windowTitle)
        {
            IntPtr foundHwnd = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                string windowText = GetAppWindowText(hWnd);
                if (windowText.Contains(windowTitle, StringComparison.OrdinalIgnoreCase))
                {
                    if (!IsWindowVisible(hWnd))
                        return true; // skip invisible windows

                    foundHwnd = hWnd;
                    return false; // stop enumeration
                }
                return true;
            }, IntPtr.Zero);


            RECT rect = new RECT();
            GetWindowRect(foundHwnd, ref rect);
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }


        public static IReadOnlyList<string> GetApplicationWindowNames()
        {
            Collection<string> windowNames = new Collection<string>();
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    string title = GetAppWindowText(hWnd);
                    if (!string.IsNullOrWhiteSpace(title))
                        windowNames.Add(title);
                }
                return true;

            }, IntPtr.Zero);


            return windowNames;
        }

    }
}
