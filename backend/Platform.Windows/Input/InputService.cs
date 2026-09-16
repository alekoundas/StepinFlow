using Core.Enums;
using Core.Enums.Business;
using Core.Ports;
using Platform.Windows.Native;
using SharpHook;
using SharpHook.Data;
using System.Drawing;

namespace Platform.Windows.Input
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
        public bool MoveCursor(int x, int y) => NativeCursor.MoveCursor(x, y);

        public void SimulateMouseClick(int x, int y, CursorButtonTypeEnum button)
        {
            MouseButton pressed = SharpHookMap.Button(button);

            MoveCursor(x, y);
            Thread.Sleep(MoveSettleMilliseconds);

            _simulator.SimulateMousePress(pressed);
            _simulator.SimulateMouseRelease(pressed);
        }

        public void SimulateMouseDoubleClick(int x, int y, CursorButtonTypeEnum button)
        {
            MouseButton pressed = SharpHookMap.Button(button);

            MoveCursor(x, y);
            Thread.Sleep(MoveSettleMilliseconds);

            _simulator.SimulateMousePress(pressed);
            _simulator.SimulateMouseRelease(pressed);

            // Two clicks, not one long one. Windows pairs them into a double click by the gap
            // between them, so this has to be short enough to count and long enough to be two.
            Thread.Sleep(DoubleClickGapMilliseconds);

            _simulator.SimulateMousePress(pressed);
            _simulator.SimulateMouseRelease(pressed);
        }

        public void SimulateMouseDown(int x, int y, CursorButtonTypeEnum button)
        {
            MouseButton pressed = SharpHookMap.Button(button);

            MoveCursor(x, y);
            Thread.Sleep(MoveSettleMilliseconds);

            _simulator.SimulateMousePress(pressed);
        }

        public void SimulateMouseUp(int x, int y, CursorButtonTypeEnum button)
        {
            MouseButton pressed = SharpHookMap.Button(button);

            MoveCursor(x, y);
            Thread.Sleep(MoveSettleMilliseconds);

            _simulator.SimulateMouseRelease(pressed);
        }

        public void SimulateMouseScroll(int x, int y, int delta)
        {
            MoveCursor(x, y);
            Thread.Sleep(MoveSettleMilliseconds);

            _simulator.SimulateMouseWheel((short)delta, 0);
        }

        public void SimulateMouseDrag(int fromX, int fromY, int toX, int toY, CursorButtonTypeEnum button)
        {
            MouseButton pressed = SharpHookMap.Button(button);

            MoveCursor(fromX, fromY);
            Thread.Sleep(MoveSettleMilliseconds);

            _simulator.SimulateMousePress(pressed);
            Thread.Sleep(MoveSettleMilliseconds);

            // Applications that follow a drag need to see the cursor move while the button is down, so the jump is broken into steps rather than teleporting to the end.
            MoveInSteps(fromX, fromY, toX, toY);

            _simulator.SimulateMouseRelease(pressed);
        }

        public Point CursorPosition()
        {
            return NativeCursor.CurrentPosition();
        }

        public void SimulateKeyboard(string text) => _simulator.SimulateTextEntry(text);
        public void SimulateKeyPress(KeyCodeEnum key)
        {
            KeyCode pressed = SharpHookMap.Key(key);

            _simulator.SimulateKeyPress(pressed);
            _simulator.SimulateKeyRelease(pressed);
        }

        public void SimulateKeyCombination(IReadOnlyList<KeyCodeEnum> modifiers, KeyCodeEnum key)
        {
            List<KeyCode> held = modifiers.Select(SharpHookMap.Key).ToList();
            KeyCode pressed = SharpHookMap.Key(key);

            foreach (KeyCode modifier in held)
                _simulator.SimulateKeyPress(modifier);

            _simulator.SimulateKeyPress(pressed);
            _simulator.SimulateKeyRelease(pressed);

            // Let go in the order a hand would, so nothing is left held if the app is watching the
            // modifiers rather than the combination.
            for (int i = held.Count - 1; i >= 0; i--)
                _simulator.SimulateKeyRelease(held[i]);
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
