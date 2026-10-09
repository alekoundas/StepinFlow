using Business.Recording.Models;
using Core.Enums.Business;

namespace Business.Recording.Catalogs
{
    /// <summary>
    /// Every key that types a character, once. A key missing from here does something instead -
    /// Enter, Tab, an arrow - and stands on its own as a key press.
    ///
    /// Punctuation is the key's own symbol, never the one Shift gives: which symbol sits above a
    /// key belongs to the keyboard layout, and guessing it wrong is worse than the tester fixing
    /// the text where they can see it.
    /// </summary>
    public static class KeyCharacterCatalog
    {
        public static readonly HashSet<KeyCodeEnum> ModifierKeys =
        [
            KeyCodeEnum.LeftShift, KeyCodeEnum.RightShift,
            KeyCodeEnum.LeftCtrl, KeyCodeEnum.RightCtrl,
            KeyCodeEnum.LeftAlt, KeyCodeEnum.RightAlt,
            KeyCodeEnum.LeftMeta, KeyCodeEnum.RightMeta,
            KeyCodeEnum.CapsLock, KeyCodeEnum.NumLock,
        ];

        public static readonly HashSet<KeyCodeEnum> CombiningModifiers =
        [
            KeyCodeEnum.LeftCtrl, KeyCodeEnum.RightCtrl,
            KeyCodeEnum.LeftAlt, KeyCodeEnum.RightAlt,
            KeyCodeEnum.LeftMeta, KeyCodeEnum.RightMeta,
        ];

        // The modifiers a person holds, by the name a shortcut writes them with: left and right are
        // one, because nobody writes "LeftCtrl+C". Caps Lock and Num Lock are toggles, not held.
        public static readonly Dictionary<KeyCodeEnum, string> ModifierNames = new Dictionary<KeyCodeEnum, string>
        {
            { KeyCodeEnum.LeftCtrl, "Ctrl" },
            { KeyCodeEnum.RightCtrl, "Ctrl" },
            { KeyCodeEnum.LeftAlt, "Alt" },
            { KeyCodeEnum.RightAlt, "Alt" },
            { KeyCodeEnum.LeftShift, "Shift" },
            { KeyCodeEnum.RightShift, "Shift" },
            { KeyCodeEnum.LeftMeta, "Win" },
            { KeyCodeEnum.RightMeta, "Win" },
        };

        // The order a shortcut is written in, whatever order the keys went down: Ctrl+Shift+S.
        public static readonly IReadOnlyList<string> ModifierOrder = ["Ctrl", "Alt", "Shift", "Win"];

        public static IReadOnlyList<KeyCharacter> All { get; } =
        [
            // Letters
            new KeyCharacter(KeyCodeEnum.A, "a", "A"),
            new KeyCharacter(KeyCodeEnum.B, "b", "B"),
            new KeyCharacter(KeyCodeEnum.C, "c", "C"),
            new KeyCharacter(KeyCodeEnum.D, "d", "D"),
            new KeyCharacter(KeyCodeEnum.E, "e", "E"),
            new KeyCharacter(KeyCodeEnum.F, "f", "F"),
            new KeyCharacter(KeyCodeEnum.G, "g", "G"),
            new KeyCharacter(KeyCodeEnum.H, "h", "H"),
            new KeyCharacter(KeyCodeEnum.I, "i", "I"),
            new KeyCharacter(KeyCodeEnum.J, "j", "J"),
            new KeyCharacter(KeyCodeEnum.K, "k", "K"),
            new KeyCharacter(KeyCodeEnum.L, "l", "L"),
            new KeyCharacter(KeyCodeEnum.M, "m", "M"),
            new KeyCharacter(KeyCodeEnum.N, "n", "N"),
            new KeyCharacter(KeyCodeEnum.O, "o", "O"),
            new KeyCharacter(KeyCodeEnum.P, "p", "P"),
            new KeyCharacter(KeyCodeEnum.Q, "q", "Q"),
            new KeyCharacter(KeyCodeEnum.R, "r", "R"),
            new KeyCharacter(KeyCodeEnum.S, "s", "S"),
            new KeyCharacter(KeyCodeEnum.T, "t", "T"),
            new KeyCharacter(KeyCodeEnum.U, "u", "U"),
            new KeyCharacter(KeyCodeEnum.V, "v", "V"),
            new KeyCharacter(KeyCodeEnum.W, "w", "W"),
            new KeyCharacter(KeyCodeEnum.X, "x", "X"),
            new KeyCharacter(KeyCodeEnum.Y, "y", "Y"),
            new KeyCharacter(KeyCodeEnum.Z, "z", "Z"),

            // Number row
            new KeyCharacter(KeyCodeEnum.Num0, "0"),
            new KeyCharacter(KeyCodeEnum.Num1, "1"),
            new KeyCharacter(KeyCodeEnum.Num2, "2"),
            new KeyCharacter(KeyCodeEnum.Num3, "3"),
            new KeyCharacter(KeyCodeEnum.Num4, "4"),
            new KeyCharacter(KeyCodeEnum.Num5, "5"),
            new KeyCharacter(KeyCodeEnum.Num6, "6"),
            new KeyCharacter(KeyCodeEnum.Num7, "7"),
            new KeyCharacter(KeyCodeEnum.Num8, "8"),
            new KeyCharacter(KeyCodeEnum.Num9, "9"),

            // Numpad
            new KeyCharacter(KeyCodeEnum.Numpad0, "0"),
            new KeyCharacter(KeyCodeEnum.Numpad1, "1"),
            new KeyCharacter(KeyCodeEnum.Numpad2, "2"),
            new KeyCharacter(KeyCodeEnum.Numpad3, "3"),
            new KeyCharacter(KeyCodeEnum.Numpad4, "4"),
            new KeyCharacter(KeyCodeEnum.Numpad5, "5"),
            new KeyCharacter(KeyCodeEnum.Numpad6, "6"),
            new KeyCharacter(KeyCodeEnum.Numpad7, "7"),
            new KeyCharacter(KeyCodeEnum.Numpad8, "8"),
            new KeyCharacter(KeyCodeEnum.Numpad9, "9"),

            // Space and punctuation
            new KeyCharacter(KeyCodeEnum.Space, " "),
            new KeyCharacter(KeyCodeEnum.Comma, ","),
            new KeyCharacter(KeyCodeEnum.Period, "."),
            new KeyCharacter(KeyCodeEnum.Slash, "/"),
            new KeyCharacter(KeyCodeEnum.Backslash, "\\"),
            new KeyCharacter(KeyCodeEnum.Semicolon, ";"),
            new KeyCharacter(KeyCodeEnum.Quote, "'"),
            new KeyCharacter(KeyCodeEnum.BracketLeft, "["),
            new KeyCharacter(KeyCodeEnum.BracketRight, "]"),
            new KeyCharacter(KeyCodeEnum.Minus, "-"),
            new KeyCharacter(KeyCodeEnum.Equal, "="),
            new KeyCharacter(KeyCodeEnum.Backtick, "`"),
        ];


        /// <summary>
        /// What the key types, its capital when <paramref name="isCapital"/> and it has one.
        /// </summary>
        public static string? GetCharacter(KeyCodeEnum keyCode, bool isCapital)
        {
            if (!All.ToDictionary(x => x.KeyCode).TryGetValue(keyCode, out KeyCharacter? key))
                return null;

            if (isCapital && key.Capital != null)
                return key.Capital;

            return key.Character;
        }
    }
}
