using SharpHook.Data;

namespace Business.Services.InputService
{
    public interface IInputService
    {
        public void SimulateMouseClick(int x, int y, MouseButton button);
        public void SimulateMouseScroll(int x, int y, int delta);

        public void SimulateKeyboard(string text);
        public void SimulateKeyPress(KeyCode key);
    }
}
