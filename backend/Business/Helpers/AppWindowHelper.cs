using Core.Enums;
using Core.Models.Business;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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
        // P/Invoke Get the window the user is actually in
        //==================================================
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private const int SW_RESTORE = 9;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);


        public static string? GetForegroundWindowTitle()
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
                return null;

            string title = GetAppWindowText(hWnd);
            return string.IsNullOrWhiteSpace(title) ? null : title;
        }


        //==================================================
        // P/Invoke Get window name (max 512 chars)
        //==================================================
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, [Out] char[] lpString, int nMaxCount);

        // Reuse buffer to reduce allocations. ThreadStatic because MediatR runs handlers on the
        // thread pool: a shared buffer lets two concurrent lookups read each other's titles.
        [ThreadStatic]
        private static char[]? _titleBuffer;

        private static string GetAppWindowText(IntPtr hWnd)
        {
            _titleBuffer ??= new char[512];

            int length = GetWindowText(hWnd, _titleBuffer, _titleBuffer.Length);
            return new string(_titleBuffer, 0, length);
        }


        //==================================================
        // P/Invoke Get window size (RECT)
        //==================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);




        //==================================================
        // Public methods
        //==================================================

        /// <summary>
        /// Windows matching the query, in z-order. Empty when nothing matches.
        /// </summary>
        public static IReadOnlyList<IntPtr> FindWindows(WindowQuery query)
        {
            List<IntPtr> matches = new List<IntPtr>();

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                string title = GetAppWindowText(hWnd);
                if (string.IsNullOrWhiteSpace(title))
                    return true;

                if (!string.IsNullOrWhiteSpace(query.ProcessName)
                    && !string.Equals(GetProcessName(hWnd), query.ProcessName, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (!string.IsNullOrWhiteSpace(query.TitlePattern)
                    && !IsTitleMatch(title, query.TitlePattern, query.TitleMatchMode))
                    return true;

                matches.Add(hWnd);
                return true;

            }, IntPtr.Zero);

            return matches;
        }

        public static IntPtr FindWindow(WindowQuery query)
        {
            IReadOnlyList<IntPtr> matches = FindWindows(query);

            return matches.Count == 0 ? IntPtr.Zero : matches[0];
        }

        /// <summary>
        /// Bounds in physical pixels. The client area excludes the title bar and borders, so a
        /// stored offset means the same thing whatever chrome the window happens to have.
        /// Returns empty when the handle is not a live window.
        /// </summary>
        public static Rectangle GetWindowBounds(IntPtr hWnd, bool useClientArea)
        {
            if (hWnd == IntPtr.Zero)
                return Rectangle.Empty;

            if (!useClientArea)
            {
                RECT windowRect = new RECT();
                if (!GetWindowRect(hWnd, ref windowRect))
                    return Rectangle.Empty;

                return Rectangle.FromLTRB(windowRect.Left, windowRect.Top, windowRect.Right, windowRect.Bottom);
            }

            RECT clientRect = new RECT();
            if (!GetClientRect(hWnd, ref clientRect))
                return Rectangle.Empty;

            POINT origin = new POINT { X = clientRect.Left, Y = clientRect.Top };
            if (!ClientToScreen(hWnd, ref origin))
                return Rectangle.Empty;

            return new Rectangle(origin.X, origin.Y, clientRect.Right - clientRect.Left, clientRect.Bottom - clientRect.Top);
        }

        public static bool FocusWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            if (IsIconic(hWnd))
                ShowWindow(hWnd, SW_RESTORE);

            if (!SetForegroundWindow(hWnd))
                return false;

            return GetForegroundWindow() == hWnd;
        }

        /// <summary>The outer frame, title bar and borders included.</summary>
        public static bool ResizeWindow(IntPtr hWnd, int width, int height)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            if (IsIconic(hWnd))
                ShowWindow(hWnd, SW_RESTORE);

            return SetWindowPos(hWnd, IntPtr.Zero, 0, 0, width, height, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        /// <summary>The top left of the outer frame lands on the point.</summary>
        public static bool MoveWindow(IntPtr hWnd, int x, int y)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            if (IsIconic(hWnd))
                ShowWindow(hWnd, SW_RESTORE);

            return SetWindowPos(hWnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        public static IReadOnlyList<WindowMatch> FindWindowMatches(WindowQuery query) =>
            FindWindows(query)
                .Select(hWnd => new WindowMatch
                {
                    Title = GetAppWindowText(hWnd),
                    ProcessName = GetProcessName(hWnd),
                    Bounds = GetWindowBounds(hWnd, query.UseClientArea),
                })
                .ToList();

        public static IReadOnlyList<SystemWindow> GetApplicationWindows()
        {
            Collection<SystemWindow> windows = new Collection<SystemWindow>();

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                string title = GetAppWindowText(hWnd);
                if (string.IsNullOrWhiteSpace(title))
                    return true;

                GetWindowThreadProcessId(hWnd, out uint processId);

                windows.Add(new SystemWindow
                {
                    Title = title,
                    ProcessName = GetProcessName(hWnd),
                    ProcessId = (int)processId,
                });

                return true;

            }, IntPtr.Zero);

            return windows;
        }



        // ================================================================
        // Private methods
        // ================================================================

        private static string GetProcessName(IntPtr hWnd)
        {
            GetWindowThreadProcessId(hWnd, out uint processId);

            try
            {
                using Process process = Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch
            {
                // Process exited between the enumeration and the lookup.
                return string.Empty;
            }
        }

        private static bool IsTitleMatch(string title, string pattern, TitleMatchModeEnum mode)
        {
            switch (mode)
            {
                case TitleMatchModeEnum.EQUALS:
                    return string.Equals(title, pattern, StringComparison.OrdinalIgnoreCase);

                case TitleMatchModeEnum.STARTS_WITH:
                    return title.StartsWith(pattern, StringComparison.OrdinalIgnoreCase);

                case TitleMatchModeEnum.REGEX:
                    try
                    {
                        return Regex.IsMatch(title, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
                    }
                    catch (ArgumentException)
                    {
                        return false; // User typed an invalid pattern.
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        return false;
                    }

                case TitleMatchModeEnum.CONTAINS:
                default:
                    return title.Contains(pattern, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
