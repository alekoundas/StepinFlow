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
    /// The same on every system, over SharpHook; what each system does its own way is IMouseService.
    /// </summary>
    public interface IInputSimulatorService
    {
        Point CursorPosition();

        bool MoveCursor(int x, int y);

        void SimulateMouseClick(int x, int y, CursorButtonTypeEnum button);
        void SimulateMouseDoubleClick(int x, int y, CursorButtonTypeEnum button);
        void SimulateMouseDown(int x, int y, CursorButtonTypeEnum button);
        void SimulateMouseUp(int x, int y, CursorButtonTypeEnum button);
        /// <summary>Turns the wheel whole notches, or tilts it for left and right. Each platform turns a notch into its own wheel units.</summary>
        void SimulateMouseScroll(int x, int y, CursorScrollDirectionTypeEnum direction, int notches, CancellationToken ct);
        void SimulateMouseDrag(int fromX, int fromY, int toX, int toY, CursorButtonTypeEnum button);

        void SimulateKeyboard(string text);
        void SimulateKeyPress(KeyCodeEnum key);
        void SimulateKeyCombination(IReadOnlyList<KeyCodeEnum> modifiers, KeyCodeEnum key);

        /// <summary>Puts the keys down in order and leaves them down, for the steps that follow.</summary>
        void SimulateKeysDown(IReadOnlyList<KeyCodeEnum> keys);

        /// <summary>Lets the keys back up, last down first up.</summary>
        void SimulateKeysUp(IReadOnlyList<KeyCodeEnum> keys);

        /// <summary>
        /// Lets go of every key and mouse button a step put down and none let go of. What an
        /// execution does as it ends, however it ends, so a failure between a hold and its release
        /// does not leave Ctrl down on the machine.
        /// </summary>
        void ReleaseHeld();
    }
}
