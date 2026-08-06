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
        public void SimulateMouseScroll(int x, int y, int delta);

        public void SimulateKeyboard(string text);
        public void SimulateKeyPress(KeyCode key);
    }
}
