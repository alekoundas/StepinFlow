using Core.Enums;
using Core.Enums.Business;
using SharpHook.Data;

namespace Platform.Windows.Input
{
    // This application's vocabulary translated into the input library's, in the one place that is
    // allowed to know the library exists. Everything above the port speaks KeyCodeEnum and
    // CursorButtonTypeEnum; swapping SharpHook for something else is a rewrite of this file.
    internal static class SharpHookMap
    {
        internal static MouseButton Button(CursorButtonTypeEnum button)
        {
            switch (button)
            {
                case CursorButtonTypeEnum.RIGHT_BUTTON: return MouseButton.Button2;
                case CursorButtonTypeEnum.MIDDLE_BUTTON: return MouseButton.Button3;
                default: return MouseButton.Button1;
            }
        }

        // Undefined rather than an exception: a key nothing can press is a step that does nothing,
        // which the validator catches, where a throw would take down an execution mid flow.
        internal static KeyCode Key(KeyCodeEnum key)
        {
            switch (key)
            {
                case KeyCodeEnum.Num0: return KeyCode.Vc0;
                case KeyCodeEnum.Num1: return KeyCode.Vc1;
                case KeyCodeEnum.Num2: return KeyCode.Vc2;
                case KeyCodeEnum.Num3: return KeyCode.Vc3;
                case KeyCodeEnum.Num4: return KeyCode.Vc4;
                case KeyCodeEnum.Num5: return KeyCode.Vc5;
                case KeyCodeEnum.Num6: return KeyCode.Vc6;
                case KeyCodeEnum.Num7: return KeyCode.Vc7;
                case KeyCodeEnum.Num8: return KeyCode.Vc8;
                case KeyCodeEnum.Num9: return KeyCode.Vc9;

                case KeyCodeEnum.NumpadEnter: return KeyCode.VcNumPadEnter;
                case KeyCodeEnum.NumpadPlus: return KeyCode.VcNumPadAdd;
                case KeyCodeEnum.NumpadMinus: return KeyCode.VcNumPadSubtract;
                case KeyCodeEnum.NumpadMultiply: return KeyCode.VcNumPadMultiply;
                case KeyCodeEnum.NumpadDivide: return KeyCode.VcNumPadDivide;

                case KeyCodeEnum.ArrowUp: return KeyCode.VcUp;
                case KeyCodeEnum.ArrowDown: return KeyCode.VcDown;
                case KeyCodeEnum.ArrowLeft: return KeyCode.VcLeft;
                case KeyCodeEnum.ArrowRight: return KeyCode.VcRight;

                case KeyCodeEnum.BracketLeft: return KeyCode.VcOpenBracket;
                case KeyCodeEnum.BracketRight: return KeyCode.VcCloseBracket;
                case KeyCodeEnum.Backtick: return KeyCode.VcBackQuote;
                case KeyCodeEnum.Equal: return KeyCode.VcEquals;

                case KeyCodeEnum.LeftCtrl: return KeyCode.VcLeftControl;
                case KeyCodeEnum.RightCtrl: return KeyCode.VcRightControl;

                case KeyCodeEnum.Unknown: return KeyCode.VcUndefined;

                default:
                    break;
            }

            // Numpad0 to Numpad9 differ only in case, and everything left over is the member name
            // with the prefix on it.
            if (key >= KeyCodeEnum.Numpad0 && key <= KeyCodeEnum.Numpad9)
                return Enum.Parse<KeyCode>($"VcNumPad{(int)(key - KeyCodeEnum.Numpad0)}");

            return Enum.TryParse($"Vc{key}", out KeyCode code) ? code : KeyCode.VcUndefined;
        }
    }
}
