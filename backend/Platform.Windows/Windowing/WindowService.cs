using Core.Helpers;
using Core.Models.Business;
using Core.Ports;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Platform.Windows.Windowing
{
    public sealed partial class WindowService : IWindowService
    {

        //==================================================
        // P/Invoke Ask a window to close
        //==================================================
        [LibraryImport("user32.dll", EntryPoint = "PostMessageW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private const uint WM_CLOSE = 0x0010;


        //==================================================
        // P/Invoke See if window is visible
        //==================================================
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsWindowVisible(IntPtr hWnd);


        //==================================================
        // P/Invoke Get all app windows
        //==================================================
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);


        //==================================================
        // P/Invoke Get the window the user is actually in
        //==================================================
        [LibraryImport("user32.dll")]
        private static partial IntPtr GetForegroundWindow();

        private const int SW_RESTORE = 9;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetForegroundWindow(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsIconic(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);


        public string? GetForegroundWindowTitle()
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

        // Reuse buffer to reduce allocations. ThreadStatic because the IPC pipe runs handlers on the
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

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetClientRect(IntPtr hWnd, ref RECT lpRect);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [LibraryImport("user32.dll")]
        private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);




        //==================================================
        // Public methods
        //==================================================

        /// <summary>
        /// Windows matching the query, in z-order. Empty when nothing matches.
        /// </summary>
        public IReadOnlyList<WindowHandle> FindWindows(WindowQuery query)
        {
            List<WindowHandle> matches = new List<WindowHandle>();

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                if (WindowMatcherHelper.Matches(GetAppWindowText(hWnd), GetProcessName(hWnd), query))
                    matches.Add(new WindowHandle(hWnd));

                return true;

            }, IntPtr.Zero);

            return matches;
        }

        public WindowHandle FindWindow(WindowQuery query)
        {
            IReadOnlyList<WindowHandle> matches = FindWindows(query);

            return matches.Count == 0 ? WindowHandle.None : matches[0];
        }

        /// <summary>
        /// Bounds in physical pixels. The client area excludes the title bar and borders, so a
        /// stored offset means the same thing whatever chrome the window happens to have.
        /// Returns empty when the handle is not a live window.
        /// </summary>
        public Rectangle GetWindowBounds(WindowHandle handle, bool useClientArea)
        {
            IntPtr hWnd = handle.Value;

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

        public bool FocusWindow(WindowHandle handle)
        {
            IntPtr hWnd = handle.Value;

            if (hWnd == IntPtr.Zero)
                return false;

            if (IsIconic(hWnd))
                ShowWindow(hWnd, SW_RESTORE);

            if (!SetForegroundWindow(hWnd))
                return false;

            return GetForegroundWindow() == hWnd;
        }

        public bool ResizeWindow(WindowHandle handle, int width, int height)
        {
            IntPtr hWnd = handle.Value;

            if (hWnd == IntPtr.Zero)
                return false;

            if (IsIconic(hWnd))
                ShowWindow(hWnd, SW_RESTORE);

            return SetWindowPos(hWnd, IntPtr.Zero, 0, 0, width, height, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        public bool MoveWindow(WindowHandle handle, int x, int y)
        {
            IntPtr hWnd = handle.Value;

            if (hWnd == IntPtr.Zero)
                return false;

            if (IsIconic(hWnd))
                ShowWindow(hWnd, SW_RESTORE);

            return SetWindowPos(hWnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        /// <summary>
        /// Asks the window to close, the way clicking its X does. Posted rather than sent, so an
        /// application that puts up "are you sure" cannot block the caller for ever.
        /// </summary>
        public bool CloseWindow(WindowHandle handle)
        {
            IntPtr hWnd = handle.Value;

            if (hWnd == IntPtr.Zero)
                return false;

            return PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        public IReadOnlyList<WindowMatch> FindWindowMatches(WindowQuery query)
        {
            return FindWindows(query)
                .Select(window => new WindowMatch
                {
                    Title = GetAppWindowText(window.Value),
                    ProcessName = GetProcessName(window.Value),
                    Bounds = GetWindowBounds(window, query.UseClientArea),
                })
                .ToList();
        }

        public IReadOnlyList<SystemWindow> GetApplicationWindows()
        {
            Collection<SystemWindow> windows = new Collection<SystemWindow>();

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                string title = GetAppWindowText(hWnd);
                if (string.IsNullOrWhiteSpace(title))
                    return true;

                _ = GetWindowThreadProcessId(hWnd, out uint processId);

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
            _ = GetWindowThreadProcessId(hWnd, out uint processId);

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

    }
}
