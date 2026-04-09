using SharpHook;
using SharpHook.Data;

namespace Business.Services.InputService
{
    public sealed class InputService: IInputService
    {
        // SharpHook
        private readonly IGlobalHook _hook = new TaskPoolGlobalHook();
        private readonly IEventSimulator _simulator = new EventSimulator();

        public void SimulateMouseClick(int x, int y, MouseButton button)
        {
            _simulator.SimulateMouseMovement((short)x, (short)y);
            _simulator.SimulateMousePress(button);
            _simulator.SimulateMouseRelease(button);
        }

        public void SimulateMouseScroll(int x, int y, int delta)
        {
            _simulator.SimulateMouseMovement((short)x, (short)y);
            _simulator.SimulateMouseWheel((short)delta, 0);
        }

        public void SimulateKeyboard(string text) => _simulator.SimulateTextEntry(text);
        public void SimulateKeyPress(KeyCode key)
        {
            _simulator.SimulateKeyPress(key);
            _simulator.SimulateKeyRelease(key);
        }

        public void Dispose() => _hook.Dispose();
    }
}
