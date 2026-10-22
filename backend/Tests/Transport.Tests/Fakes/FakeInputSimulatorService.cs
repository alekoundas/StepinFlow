using System.Drawing;

using Core.Enums;
using Core.Enums.Business;
using Core.Ports;

namespace Transport.Tests.Fakes
{
    /// <summary>
    /// The cursor as a list of where it was moved and dragged, one line each. Anything else it is
    /// told to do fails the test.
    /// </summary>
    public sealed class FakeInputSimulatorService : IInputSimulatorService
    {
        public List<string> Actions { get; } = new List<string>();

        public bool MoveCursor(int x, int y)
        {
            Actions.Add($"move {x},{y}");
            return true;
        }

        public void SimulateMouseDrag(int fromX, int fromY, int toX, int toY, CursorButtonTypeEnum button)
        {
            Actions.Add($"drag {fromX},{fromY} to {toX},{toY} {button}");
        }

        public Point CursorPosition()
        {
            throw new NotImplementedException();
        }

        public void SimulateMouseClick(int x, int y, CursorButtonTypeEnum button)
        {
            throw new NotImplementedException();
        }

        public void SimulateMouseDoubleClick(int x, int y, CursorButtonTypeEnum button)
        {
            throw new NotImplementedException();
        }

        public void SimulateMouseDown(int x, int y, CursorButtonTypeEnum button)
        {
            throw new NotImplementedException();
        }

        public void SimulateMouseUp(int x, int y, CursorButtonTypeEnum button)
        {
            throw new NotImplementedException();
        }

        public void SimulateMouseScroll(int x, int y, int delta)
        {
            throw new NotImplementedException();
        }

        public void SimulateKeyboard(string text)
        {
            throw new NotImplementedException();
        }

        public void SimulateKeyPress(KeyCodeEnum key)
        {
            throw new NotImplementedException();
        }

        public void SimulateKeyCombination(IReadOnlyList<KeyCodeEnum> modifiers, KeyCodeEnum key)
        {
            throw new NotImplementedException();
        }

        public void SimulateKeysDown(IReadOnlyList<KeyCodeEnum> keys)
        {
            throw new NotImplementedException();
        }

        public void SimulateKeysUp(IReadOnlyList<KeyCodeEnum> keys)
        {
            throw new NotImplementedException();
        }

        public void ReleaseHeld()
        {
            throw new NotImplementedException();
        }
    }
}
