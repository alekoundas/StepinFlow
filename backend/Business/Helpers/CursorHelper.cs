using System.Runtime.InteropServices;

namespace Business.Helpers
{
    /// <summary>
    /// Cursor movement in physical (real device) pixels, which is the space the process runs in
    /// now that it is Per-Monitor-V2 aware.
    /// </summary>
    public static class CursorHelper
    {
        //==================================================
        // P/Invoke Virtual screen metrics
        //==================================================
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);


        //==================================================
        // P/Invoke SendInput
        //==================================================
        private const uint INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);




        //==================================================
        // Public methods
        //==================================================

        /// <summary>
        /// Moves the cursor to an absolute point on the virtual desktop, in physical pixels.
        /// Returns false when the virtual screen metrics cannot be read.
        /// </summary>
        public static bool MoveCursor(int x, int y)
        {
            int virtualLeft = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int virtualTop = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int virtualWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int virtualHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            if (virtualWidth <= 1 || virtualHeight <= 1)
                return false;

            // Clamp first: a stale FlowPoint from a machine with a bigger desktop would
            // otherwise wrap to the opposite edge instead of stopping at the border.
            x = Math.Clamp(x, virtualLeft, virtualLeft + virtualWidth - 1);
            y = Math.Clamp(y, virtualTop, virtualTop + virtualHeight - 1);

            // SendInput absolute coordinates are 0..65535 across the whole virtual desktop.
            INPUT[] inputs =
            [
                new INPUT
                {
                    type = INPUT_MOUSE,
                    mi = new MOUSEINPUT
                    {
                        dx = (int)((long)(x - virtualLeft) * 65535 / (virtualWidth - 1)),
                        dy = (int)((long)(y - virtualTop) * 65535 / (virtualHeight - 1)),
                        dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK,
                    },
                },
            ];

            return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>()) == inputs.Length;
        }
    }
}
