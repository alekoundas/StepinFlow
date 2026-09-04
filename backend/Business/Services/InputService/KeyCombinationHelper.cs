using Core.Enums.Business;

using SharpHook.Data;

namespace Business.Services.InputService
{
    /// <summary>
    /// Turns "Ctrl+V" back into keys.
    ///
    /// The recorder writes a combination as the text a person would read, because that is what the
    /// step shows and what someone editing it types. Nothing else stores the keys, so this is where
    /// the reading turns back into something that can be pressed.
    /// </summary>
    public static class KeyCombinationHelper
    {
        public static bool TryParse(string text, out List<KeyCode> modifiers, out KeyCode key)
        {
            modifiers = new List<KeyCode>();
            key = KeyCode.VcUndefined;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            List<string> parts = text
                .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (parts.Count == 0)
                return false;

            for (int i = 0; i < parts.Count - 1; i++)
            {
                KeyCode? modifier = Modifier(parts[i]);
                if (modifier == null)
                    return false;

                modifiers.Add(modifier.Value);
            }

            KeyCode? pressed = Key(parts[^1]);
            if (pressed == null)
                return false;

            key = pressed.Value;

            return true;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Left and right are one shortcut to a reader, and the left one is what a keyboard sends
        // when nobody said which.
        private static KeyCode? Modifier(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    return KeyCode.VcLeftControl;

                case "alt":
                    return KeyCode.VcLeftAlt;

                case "shift":
                    return KeyCode.VcLeftShift;

                case "win":
                case "meta":
                case "cmd":
                    return KeyCode.VcLeftMeta;

                default:
                    return null;
            }
        }

        // Named the way the recorder wrote it, which is the KeyCodeEnum member.
        private static KeyCode? Key(string name)
        {
            if (!Enum.TryParse(name, true, out KeyCodeEnum parsed))
                return null;

            switch (parsed)
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

                case KeyCodeEnum.Unknown: return null;

                default:
                    break;
            }

            // Numpad0 to Numpad9 differ only in case, and everything left over is the member name
            // with the prefix on it.
            if (parsed >= KeyCodeEnum.Numpad0 && parsed <= KeyCodeEnum.Numpad9)
                return Enum.Parse<KeyCode>($"VcNumPad{(int)(parsed - KeyCodeEnum.Numpad0)}");

            return Enum.TryParse($"Vc{parsed}", out KeyCode code) ? code : null;
        }
    }
}
