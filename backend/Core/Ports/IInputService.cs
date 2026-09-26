using Core.Enums;
using Core.Enums.Business;
using System.Drawing;

namespace Core.Ports
{
    // Dependency Inversion Principle(DIP)


    /// <summary>
    /// Driving the real mouse and keyboard.
    /// Points are absolute.
    /// 
    /// Will use the Platform.Windows or Platform.Linux depending the underlying OS
    /// </summary>
    public interface IInputService
    {
        Point CursorPosition();

        bool MoveCursor(int x, int y);

        void SimulateMouseClick(int x, int y, CursorButtonTypeEnum button);
        void SimulateMouseDoubleClick(int x, int y, CursorButtonTypeEnum button);
        void SimulateMouseDown(int x, int y, CursorButtonTypeEnum button);
        void SimulateMouseUp(int x, int y, CursorButtonTypeEnum button);
        void SimulateMouseScroll(int x, int y, int delta);
        void SimulateMouseDrag(int fromX, int fromY, int toX, int toY, CursorButtonTypeEnum button);

        void SimulateKeyboard(string text);
        void SimulateKeyPress(KeyCodeEnum key);
        void SimulateKeyCombination(IReadOnlyList<KeyCodeEnum> modifiers, KeyCodeEnum key);
    }
}
