using Business.Helpers;
using SharpHook;
using SharpHook.Data;

namespace Business.Services.InputService
{
    public sealed class InputService : IInputService
    {
        // SharpHook
        private readonly IEventSimulator _simulator = new EventSimulator();

        // Target apps process the move asynchronously, so pressing in the same tick can register
        // the click at the previous position.
        private const int MoveSettleMilliseconds = 16;

        // Cursor movement does not go through SharpHook: its absolute coordinates are DPI
        // virtualized, and everything stored in the database is in physical pixels.
        public bool MoveCursor(int x, int y) => CursorHelper.MoveCursor(x, y);

        public void SimulateMouseClick(int x, int y, MouseButton button)
        {
            MoveCursor(x, y);
            Thread.Sleep(MoveSettleMilliseconds);

            _simulator.SimulateMousePress(button);
            _simulator.SimulateMouseRelease(button);
        }

        public void SimulateMouseScroll(int x, int y, int delta)
        {
            MoveCursor(x, y);
            Thread.Sleep(MoveSettleMilliseconds);

            _simulator.SimulateMouseWheel((short)delta, 0);
        }

        public void SimulateKeyboard(string text) => _simulator.SimulateTextEntry(text);
        public void SimulateKeyPress(KeyCode key)
        {
            _simulator.SimulateKeyPress(key);
            _simulator.SimulateKeyRelease(key);
        }
    }
}
