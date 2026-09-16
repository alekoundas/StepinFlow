using Core.Enums;
using Core.Enums.Business;
using System.Drawing;

namespace Core.Ports
{
    // Dependency Inversion Principle(DIP)


    /// <summary>
    /// Driving the real mouse and keyboard.
    ///
    /// Will use the Platform.Windows or Platform.Linux depending the underlying OS
    /// </summary>
    public interface IInputService
    {
        /// <summary>
        /// Moves the cursor to an absolute point on the virtual desktop, in physical pixels.
        /// </summary>
        bool MoveCursor(int x, int y);

        void SimulateMouseClick(int x, int y, CursorButtonTypeEnum button);
        void SimulateMouseDoubleClick(int x, int y, CursorButtonTypeEnum button);
        void SimulateMouseDown(int x, int y, CursorButtonTypeEnum button);
        void SimulateMouseUp(int x, int y, CursorButtonTypeEnum button);
        void SimulateMouseScroll(int x, int y, int delta);
        void SimulateMouseDrag(int fromX, int fromY, int toX, int toY, CursorButtonTypeEnum button);
        Point CursorPosition();

        void SimulateKeyboard(string text);
        void SimulateKeyPress(KeyCodeEnum key);
        void SimulateKeyCombination(IReadOnlyList<KeyCodeEnum> modifiers, KeyCodeEnum key);
    }
}
