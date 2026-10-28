using Core.Catalogs;
using Core.Enums;

namespace Core.Models.Business
{
    /// <summary>
    /// A recording gesture as its setting binds it: modifiers held and a button pressed,
    /// "VcLeftControl+LEFT_BUTTON", in the hook's names. A modifier is either side of the keyboard,
    /// as everywhere a key is named. The hook matches it to hold the press back from the
    /// application, and the recorder to leave its modifiers out.
    /// </summary>
    public sealed class PressGesture
    {
        private const char SEPARATOR = '+';

        // The hook's names for the modifier keys, by the name each is written with.
        private static readonly Dictionary<string, string> ModifiersByKeyName = new Dictionary<string, string>
        {
            ["VcLeftControl"] = KeyCatalog.CTRL,
            ["VcRightControl"] = KeyCatalog.CTRL,
            ["VcLeftAlt"] = KeyCatalog.ALT,
            ["VcRightAlt"] = KeyCatalog.ALT,
            ["VcLeftShift"] = KeyCatalog.SHIFT,
            ["VcRightShift"] = KeyCatalog.SHIFT,
            ["VcLeftMeta"] = KeyCatalog.WIN,
            ["VcRightMeta"] = KeyCatalog.WIN,
        };

        private PressGesture(RecordingGestureEnum gesture, HashSet<string> modifiers, CursorButtonTypeEnum button)
        {
            Gesture = gesture;
            Modifiers = modifiers;
            Button = button;
        }

        public RecordingGestureEnum Gesture { get; }

        /// <summary>Written as KeyCatalog names them: Ctrl, Alt, Shift, Win.</summary>
        public IReadOnlySet<string> Modifiers { get; }

        public CursorButtonTypeEnum Button { get; }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Null for an empty combination, which leaves the gesture unbound, and for one that is not
        /// modifiers and a button: a press with nothing held is a click.
        /// </summary>
        public static PressGesture? Parse(RecordingGestureEnum gesture, string combination)
        {
            string[] parts = combination.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || !Enum.TryParse(parts[^1], out CursorButtonTypeEnum button))
                return null;

            HashSet<string> modifiers = new HashSet<string>();
            foreach (string keyName in parts[..^1])
            {
                string? modifier = ModifierOf(keyName);
                if (modifier == null)
                    return null;

                modifiers.Add(modifier);
            }

            return new PressGesture(gesture, modifiers, button);
        }

        /// <summary>The modifier a key of the hook's is, or null when it is not one.</summary>
        public static string? ModifierOf(string keyName)
        {
            return ModifiersByKeyName.GetValueOrDefault(keyName);
        }

        /// <summary>Exactly these modifiers are held, and this button went down.</summary>
        public bool Matches(IReadOnlySet<string> heldModifiers, CursorButtonTypeEnum button)
        {
            return button == Button && Modifiers.SetEquals(heldModifiers);
        }
    }
}
