using System.Globalization;

using Core.Enums.Business;

namespace Business.Executions
{
    /// <summary>
    /// Turns "Ctrl+V" back into keys.
    ///
    /// The recorder writes a combination as the text a person would read, because that is what the
    /// step shows and what someone editing it types. Nothing else stores the keys, so this is where
    /// the reading turns back into something that can be pressed.
    ///
    /// Produces this application's own <see cref="KeyCodeEnum"/>. Mapping those onto whatever the
    /// input library calls them belongs to the adapter that presses them.
    /// </summary>
    public static class KeyCombinationHelper
    {
        // Left and right are one shortcut to a reader, and the left one is what a keyboard sends
        // when nobody said which.
        private static readonly Dictionary<string, KeyCodeEnum> Modifiers = new Dictionary<string, KeyCodeEnum>(StringComparer.OrdinalIgnoreCase)
        {
            ["ctrl"] = KeyCodeEnum.LeftCtrl,
            ["control"] = KeyCodeEnum.LeftCtrl,
            ["alt"] = KeyCodeEnum.LeftAlt,
            ["shift"] = KeyCodeEnum.LeftShift,
            ["win"] = KeyCodeEnum.LeftMeta,
            ["meta"] = KeyCodeEnum.LeftMeta,
            ["cmd"] = KeyCodeEnum.LeftMeta,
        };

        // Every key by the name the recorder writes, and a digit as the number key. Nothing outside
        // this table is a key.
        private static readonly Dictionary<string, KeyCodeEnum> Keys = BuildKeys();

        public static bool TryParse(string text, out List<KeyCodeEnum> modifiers, out KeyCodeEnum key)
        {
            modifiers = new List<KeyCodeEnum>();
            key = KeyCodeEnum.Unknown;

            string[] parts = (text ?? string.Empty).Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return false;

            foreach (string part in parts[..^1])
            {
                if (!Modifiers.TryGetValue(part, out KeyCodeEnum modifier))
                    return false;

                modifiers.Add(modifier);
            }

            return Keys.TryGetValue(parts[^1], out key);
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
