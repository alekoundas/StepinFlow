using System.Globalization;

using Core.Enums.Business;
using Core.Models.Business;

namespace Core.Catalogs
{
    /// <summary>
    /// The one definition of how keys are named, read by the recorder that writes "Ctrl+V" and by
    /// the execution and the validation that read it back into keys, so the two cannot disagree.
    ///
    /// Produces this application's own <see cref="KeyCodeEnum"/>. Mapping those onto whatever the
    /// input library calls them belongs to the adapter that presses them.
    /// </summary>
    public static class KeyCatalog
    {
        public const string CTRL = "Ctrl";
        public const string ALT = "Alt";
        public const string SHIFT = "Shift";
        public const string WIN = "Win";

        // Toggles. What they changed is in the characters typed.
        public static readonly HashSet<KeyCodeEnum> LockKeys =
        [
            KeyCodeEnum.CapsLock,
            KeyCodeEnum.NumLock
        ];

        // The name each modifier is written with. Left and right are one, because nobody writes "LeftCtrl+C".
        public static readonly Dictionary<KeyCodeEnum, string> ModifierNames = new Dictionary<KeyCodeEnum, string>
        {
            { KeyCodeEnum.LeftCtrl, CTRL },
            { KeyCodeEnum.RightCtrl, CTRL },
            { KeyCodeEnum.LeftAlt, ALT },
            { KeyCodeEnum.RightAlt, ALT },
            { KeyCodeEnum.LeftShift, SHIFT },
            { KeyCodeEnum.RightShift, SHIFT },
            { KeyCodeEnum.LeftMeta, WIN },
            { KeyCodeEnum.RightMeta, WIN },
        };

        // Every name a modifier is read by: the written one, and the others a person might type. The
        // left key is what a keyboard sends when nobody said which.
        private static readonly Dictionary<string, KeyCodeEnum> ModifierKeys = new Dictionary<string, KeyCodeEnum>(StringComparer.OrdinalIgnoreCase)
        {
            [CTRL] = KeyCodeEnum.LeftCtrl,
            ["control"] = KeyCodeEnum.LeftCtrl,
            [ALT] = KeyCodeEnum.LeftAlt,
            [SHIFT] = KeyCodeEnum.LeftShift,
            [WIN] = KeyCodeEnum.LeftMeta,
            ["meta"] = KeyCodeEnum.LeftMeta,
            ["cmd"] = KeyCodeEnum.LeftMeta,
        };

        // Every key by the name the recorder writes, and a digit as the number key. Nothing outside
        // this table is a key.
        private static readonly Dictionary<string, KeyCodeEnum> Keys = BuildKeys();


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Check if the recorded input is a modifier key.
        /// </summary>
        public static bool IsModifier(RecordedInput input)
        {
            return input.KeyCode != null && ModifierNames.ContainsKey(input.KeyCode.Value);
        }

        /// <summary>
        /// Check if the recorded input is a modifier key, and equal to name param.
        /// </summary>
        public static bool IsModifier(RecordedInput input, string name)
        {
            return input.KeyCode != null
                && ModifierNames.TryGetValue(input.KeyCode.Value, out string? modifier)
                && modifier == name;
        }

        /// <summary>
        /// Check whether a key typed text. Text shouldnt contain  Enter, Tab, Backspace, Escape, etc type control characters.
        /// </summary>
        public static bool IsText(string? typedCharacter)
        {
            return !string.IsNullOrEmpty(typedCharacter) && !typedCharacter.Any(char.IsControl);
        }

        /// <summary>
        /// Turns "Ctrl+V" back into keys: the modifiers, then the key they are pressed with. A
        /// modifier is a key to press too - Win on its own opens the Start menu.
        /// </summary>
        public static bool TryParse(string text, out List<KeyCodeEnum> modifiers, out KeyCodeEnum key)
        {
            modifiers = new List<KeyCodeEnum>();
            key = KeyCodeEnum.Unknown;

            string[] parts = (text ?? string.Empty).Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return false;

            foreach (string part in parts[..^1])
            {
                if (!ModifierKeys.TryGetValue(part, out KeyCodeEnum modifier))
                    return false;

                modifiers.Add(modifier);
            }

            if (ModifierKeys.TryGetValue(parts[^1], out key))
                return true;

            return Keys.TryGetValue(parts[^1], out key);
        }

        /// <summary>
        /// Every key the text names, in the order written - what a hold puts down and a release lets
        /// up. A modifier may stand anywhere, alone included: "Ctrl" and "Ctrl+Shift" are both keys
        /// to hold.
        /// </summary>
        public static bool TryParseKeys(string text, out List<KeyCodeEnum> keys)
        {
            keys = new List<KeyCodeEnum>();

            string[] parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return false;

            foreach (string part in parts)
            {
                if (ModifierKeys.TryGetValue(part, out KeyCodeEnum modifier))
                {
                    keys.Add(modifier);
                    continue;
                }

                if (!Keys.TryGetValue(part, out KeyCodeEnum key))
                    return false;

                keys.Add(key);
            }

            return true;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static Dictionary<string, KeyCodeEnum> BuildKeys()
        {
            Dictionary<string, KeyCodeEnum> keys = Enum.GetValues<KeyCodeEnum>()
                .Where(x => x != KeyCodeEnum.Unknown)
                .ToDictionary(x => x.ToString(), StringComparer.OrdinalIgnoreCase);

            for (int digit = 0; digit <= 9; digit++)
                keys[digit.ToString(CultureInfo.InvariantCulture)] = KeyCodeEnum.Num0 + digit;

            return keys;
        }
    }
}
