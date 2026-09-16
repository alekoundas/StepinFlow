using Core.Enums.Business;

namespace Core.Helpers
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
        public static bool TryParse(string text, out List<KeyCodeEnum> modifiers, out KeyCodeEnum key)
        {
            modifiers = new List<KeyCodeEnum>();
            key = KeyCodeEnum.Unknown;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            List<string> parts = text
                .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (parts.Count == 0)
                return false;

            for (int i = 0; i < parts.Count - 1; i++)
            {
                KeyCodeEnum? modifier = Modifier(parts[i]);
                if (modifier == null)
                    return false;

                modifiers.Add(modifier.Value);
            }

            KeyCodeEnum? pressed = Key(parts[^1]);
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
        private static KeyCodeEnum? Modifier(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    return KeyCodeEnum.LeftCtrl;

                case "alt":
                    return KeyCodeEnum.LeftAlt;

                case "shift":
                    return KeyCodeEnum.LeftShift;

                case "win":
                case "meta":
                case "cmd":
                    return KeyCodeEnum.LeftMeta;

                default:
                    return null;
            }
        }

        // Named the way the recorder wrote it, which is the KeyCodeEnum member.
        private static KeyCodeEnum? Key(string name)
        {
            if (!Enum.TryParse(name, true, out KeyCodeEnum parsed))
                return null;

            return parsed == KeyCodeEnum.Unknown ? null : parsed;
        }
    }
}
