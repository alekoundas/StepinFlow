using System.Drawing;

namespace Core.Ports
{
    // Dependency Inversion Principle(DIP)


    /// <summary>
    /// What the input services need from the system itself: SharpHook drives keys and buttons the
    /// same way everywhere, but has no cursor position to read, moves the cursor in DPI virtualized
    /// coordinates on Windows, and does not know how far a notch of the wheel scrolls.
    /// </summary>
    public interface IMouseService
    {
        /// <summary>Where the cursor actually is, in physical pixels.</summary>
        Point CursorPosition();

        /// <summary>Moves the cursor to an absolute point, in physical pixels. False when it could not.</summary>
        bool MoveCursor(int x, int y);

        /// <summary>The lines, or characters sideways, one notch of the wheel scrolls in the system's mouse settings.</summary>
        uint ScrollPerNotch(bool isHorizontal);
    }
}
