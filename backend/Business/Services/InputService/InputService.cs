using System.Drawing;
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

        // Well inside the Windows default of 500ms, and far enough apart that the two presses are
        // not swallowed as one.
        private const int DoubleClickGapMilliseconds = 40;

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

        public void SimulateMouseDoubleClick(int x, int y, MouseButton button)
        {
            MoveCursor(x, y);
            Thread.Sleep(MoveSettleMilliseconds);

            _simulator.SimulateMousePress(button);
            _simulator.SimulateMouseRelease(button);

            // Two clicks, not one long one. Windows pairs them into a double click by the gap
            // between them, so this has to be short enough to count and long enough to be two.
            Thread.Sleep(DoubleClickGapMilliseconds);

            _simulator.SimulateMousePress(button);
            _simulator.SimulateMouseRelease(button);
        }

        public void SimulateMouseDown(int x, int y, MouseButton button)
        {
            MoveCursor(x, y);
            Thread.Sleep(MoveSettleMilliseconds);

            _simulator.SimulateMousePress(button);
        }

        public void SimulateMouseUp(int x, int y, MouseButton button)
        {
            MoveCursor(x, y);
            Thread.Sleep(MoveSettleMilliseconds);

            _simulator.SimulateMouseRelease(button);
        }

        public void SimulateMouseScroll(int x, int y, int delta)
        {
            MoveCursor(x, y);
            Thread.Sleep(MoveSettleMilliseconds);

            _simulator.SimulateMouseWheel((short)delta, 0);
        }

        public void SimulateMouseDrag(int fromX, int fromY, int toX, int toY, MouseButton button)
        {
            MoveCursor(fromX, fromY);
            Thread.Sleep(MoveSettleMilliseconds);

            _simulator.SimulateMousePress(button);
            Thread.Sleep(MoveSettleMilliseconds);

            // Applications that follow a drag need to see the cursor move while the button is down, so the jump is broken into steps rather than teleporting to the end.
            MoveInSteps(fromX, fromY, toX, toY);

            _simulator.SimulateMouseRelease(button);
        }

        public Point CursorPosition()
        {
            return CursorHelper.CurrentPosition();
        }

        public void SimulateKeyboard(string text) => _simulator.SimulateTextEntry(text);
        public void SimulateKeyPress(KeyCode key)
        {
            _simulator.SimulateKeyPress(key);
            _simulator.SimulateKeyRelease(key);
        }

        public void SimulateKeyCombination(IReadOnlyList<KeyCode> modifiers, KeyCode key)
        {
            foreach (KeyCode modifier in modifiers)
                _simulator.SimulateKeyPress(modifier);

            _simulator.SimulateKeyPress(key);
            _simulator.SimulateKeyRelease(key);

            // Let go in the order a hand would, so nothing is left held if the app is watching the
            // modifiers rather than the combination.
            for (int i = modifiers.Count - 1; i >= 0; i--)
                _simulator.SimulateKeyRelease(modifiers[i]);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void MoveInSteps(int fromX, int fromY, int toX, int toY)
        {
            const int steps = 20;

            for (int i = 1; i <= steps; i++)
            {
                int x = fromX + (toX - fromX) * i / steps;
                int y = fromY + (toY - fromY) * i / steps;

                MoveCursor(x, y);
                Thread.Sleep(MoveSettleMilliseconds);
            }
        }
    }
}
