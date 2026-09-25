using System.Drawing;

using Core.Enums;
using Core.Enums.Business;
using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>The mouse and keyboard as a list of what they were told to do, one line each.</summary>
    public sealed class FakeInputService : IInputService
    {
        public List<string> Actions { get; } = new List<string>();
        public Point Position { get; set; }

        // False is a cursor that would not move - an elevated window in front.
        public bool CursorMoves { get; set; } = true;

        public bool MoveCursor(int x, int y)
        {
            Actions.Add($"move {x},{y}");

            if (CursorMoves)
                Position = new Point(x, y);

            return CursorMoves;
        }

        public Point CursorPosition()
        {
            return Position;
        }

        public void SimulateMouseClick(int x, int y, CursorButtonTypeEnum button)
        {
            Actions.Add($"click {x},{y} {button}");
        }

        public void SimulateMouseDoubleClick(int x, int y, CursorButtonTypeEnum button)
        {
            Actions.Add($"double click {x},{y} {button}");
        }

        public void SimulateMouseDown(int x, int y, CursorButtonTypeEnum button)
        {
            Actions.Add($"down {x},{y} {button}");
        }

        public void SimulateMouseUp(int x, int y, CursorButtonTypeEnum button)
        {
            Actions.Add($"up {x},{y} {button}");
        }

        public void SimulateMouseScroll(int x, int y, int delta)
        {
            Actions.Add($"scroll {x},{y} {delta}");
        }

        public void SimulateMouseDrag(int fromX, int fromY, int toX, int toY, CursorButtonTypeEnum button)
        {
            Actions.Add($"drag {fromX},{fromY} to {toX},{toY} {button}");
        }

        public void SimulateKeyboard(string text)
        {
            Actions.Add($"type {text}");
        }

        public void SimulateKeyPress(KeyCodeEnum key)
        {
            Actions.Add($"press {key}");
        }

        public void SimulateKeyCombination(IReadOnlyList<KeyCodeEnum> modifiers, KeyCodeEnum key)
        {
            Actions.Add($"press {string.Join("+", modifiers.Append(key))}");
        }
    }
}
