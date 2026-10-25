using Core.Ports;
using Platform.Windows.Native;
using System.Drawing;

namespace Platform.Windows.Input
{
    /// <summary>
    /// The cursor through SendInput and GetCursorPos, and the wheel settings from the Windows mouse
    /// options.
    /// </summary>
    public sealed class MouseService : IMouseService
    {
        public Point CursorPosition()
        {
            return NativeCursor.CurrentPosition();
        }

        public bool MoveCursor(int x, int y)
        {
            return NativeCursor.MoveCursor(x, y);
        }

        public uint ScrollPerNotch(bool isHorizontal)
        {
            return NativeWheel.ScrollPerNotch(isHorizontal);
        }
    }
}
