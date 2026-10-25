using Core.Enums;
using Core.Enums.Business;
using Core.Ports;
using SharpHook;
using SharpHook.Data;
using System.Drawing;

namespace Platform.Shared.Input
{
    public sealed class InputSimulatorService : IInputSimulatorService
    {
        // SharpHook
        private readonly IEventSimulator _simulator = new EventSimulator();

        // What steps put down and nothing has let go of yet, in the order it went down.
        private readonly List<KeyCode> _heldKeys = new List<KeyCode>();
        private readonly List<MouseButton> _heldButtons = new List<MouseButton>();
        private readonly Lock _heldLock = new Lock();

        // Target apps process the move asynchronously, so pressing in the same tick can register
        // the click at the previous position.
        private const int MOVE_SETTLE_MS = 16;

        // Well inside the Windows default of 500ms, and far enough apart that the two presses are
        // not swallowed as one.
        private const int DOUBLE_CLICK_GAP_MS = 40;

        // WHEEL_DELTA: one notch in Windows wheel units.
        private const short WHEEL_NOTCH = 120;

        // A real wheel sends one event per notch. Apps that count events rather than add up their
        // size take one big event as a single notch.
        private const int SCROLL_NOTCH_GAP_MS = 20;

        private readonly IMouseService _mouse;

        public InputSimulatorService(IMouseService mouse)
        {
            _mouse = mouse;
        }

        // Cursor movement does not go through SharpHook: its absolute coordinates are DPI
        // virtualized, and everything stored in the database is in physical pixels.
        public bool MoveCursor(int x, int y)
        {
            return _mouse.MoveCursor(x, y);
        }

        public void SimulateMouseClick(int x, int y, CursorButtonTypeEnum button)
        {
            MouseButton pressed = SharpHookMap.Button(button);

            MoveCursor(x, y);
            Thread.Sleep(MOVE_SETTLE_MS);

            _simulator.SimulateMousePress(pressed);
            _simulator.SimulateMouseRelease(pressed);
        }

        public void SimulateMouseDoubleClick(int x, int y, CursorButtonTypeEnum button)
        {
            MouseButton pressed = SharpHookMap.Button(button);

            MoveCursor(x, y);
            Thread.Sleep(MOVE_SETTLE_MS);

            _simulator.SimulateMousePress(pressed);
            _simulator.SimulateMouseRelease(pressed);

            // Two clicks, not one long one. Windows pairs them into a double click by the gap
            // between them, so this has to be short enough to count and long enough to be two.
            Thread.Sleep(DOUBLE_CLICK_GAP_MS);

            _simulator.SimulateMousePress(pressed);
            _simulator.SimulateMouseRelease(pressed);
        }

        public void SimulateMouseDown(int x, int y, CursorButtonTypeEnum button)
        {
            MouseButton pressed = SharpHookMap.Button(button);

            MoveCursor(x, y);
            Thread.Sleep(MOVE_SETTLE_MS);

            _simulator.SimulateMousePress(pressed);

            lock (_heldLock)
                _heldButtons.Add(pressed);
        }

        public void SimulateMouseUp(int x, int y, CursorButtonTypeEnum button)
        {
            MouseButton pressed = SharpHookMap.Button(button);

            MoveCursor(x, y);
            Thread.Sleep(MOVE_SETTLE_MS);

            _simulator.SimulateMouseRelease(pressed);

            lock (_heldLock)
                _heldButtons.Remove(pressed);
        }

        public void SimulateMouseScroll(int x, int y, CursorScrollDirectionTypeEnum direction, int notches, CancellationToken ct)
        {
            // SharpHook's sign: positive is up or left.
            bool isForward = direction == CursorScrollDirectionTypeEnum.UP || direction == CursorScrollDirectionTypeEnum.LEFT;
            short rotation = isForward ? WHEEL_NOTCH : (short)-WHEEL_NOTCH;
            MouseWheelScrollDirection wheel = SharpHookMap.Wheel(direction);

            MoveCursor(x, y);
            Thread.Sleep(MOVE_SETTLE_MS);

            for (int i = 0; i < notches; i++)
            {
                // Stop lands between notches, so a long scroll does not hold the execution up.
                if (i > 0 && ct.WaitHandle.WaitOne(SCROLL_NOTCH_GAP_MS))
                    return;

                _simulator.SimulateMouseWheel(rotation, wheel);
            }
        }

        public void SimulateMouseDrag(int fromX, int fromY, int toX, int toY, CursorButtonTypeEnum button)
        {
            MouseButton pressed = SharpHookMap.Button(button);

            MoveCursor(fromX, fromY);
            Thread.Sleep(MOVE_SETTLE_MS);

            _simulator.SimulateMousePress(pressed);
            Thread.Sleep(MOVE_SETTLE_MS);

            // Applications that follow a drag need to see the cursor move while the button is down, so the jump is broken into steps rather than teleporting to the end.
            MoveInSteps(fromX, fromY, toX, toY);

            _simulator.SimulateMouseRelease(pressed);
        }

        public Point CursorPosition()
        {
            return _mouse.CursorPosition();
        }

        public void SimulateKeyboard(string text)
        {
            _simulator.SimulateTextEntry(text);
        }

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

        public void SimulateKeysDown(IReadOnlyList<KeyCodeEnum> keys)
        {
            foreach (KeyCodeEnum key in keys)
            {
                KeyCode pressed = SharpHookMap.Key(key);
                _simulator.SimulateKeyPress(pressed);

                lock (_heldLock)
                    _heldKeys.Add(pressed);
            }
        }

        public void SimulateKeysUp(IReadOnlyList<KeyCodeEnum> keys)
        {
            for (int i = keys.Count - 1; i >= 0; i--)
            {
                KeyCode released = SharpHookMap.Key(keys[i]);
                _simulator.SimulateKeyRelease(released);

                lock (_heldLock)
                    _heldKeys.Remove(released);
            }
        }

        public void ReleaseHeld()
        {
            List<KeyCode> keys;
            List<MouseButton> buttons;
            lock (_heldLock)
            {
                keys = _heldKeys.ToList();
                buttons = _heldButtons.ToList();
                _heldKeys.Clear();
                _heldButtons.Clear();
            }

            for (int i = keys.Count - 1; i >= 0; i--)
                _simulator.SimulateKeyRelease(keys[i]);

            foreach (MouseButton button in buttons)
                _simulator.SimulateMouseRelease(button);
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
                Thread.Sleep(MOVE_SETTLE_MS);
            }
        }
    }
}
