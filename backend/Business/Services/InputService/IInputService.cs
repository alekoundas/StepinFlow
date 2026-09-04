using System.Drawing;
using SharpHook.Data;

namespace Business.Services.InputService
{
    public interface IInputService
    {
        /// <summary>
        /// Moves the cursor to an absolute point on the virtual desktop, in physical pixels.
        /// </summary>
        public bool MoveCursor(int x, int y);

        public void SimulateMouseClick(int x, int y, MouseButton button);
        public void SimulateMouseDoubleClick(int x, int y, MouseButton button);
        public void SimulateMouseDown(int x, int y, MouseButton button);
        public void SimulateMouseUp(int x, int y, MouseButton button);
        public void SimulateMouseScroll(int x, int y, int delta);
        public void SimulateMouseDrag(int fromX, int fromY, int toX, int toY, MouseButton button);
        public Point CursorPosition();

        public void SimulateKeyboard(string text);
        public void SimulateKeyPress(KeyCode key);
        public void SimulateKeyCombination(IReadOnlyList<KeyCode> modifiers, KeyCode key);
    }
}
