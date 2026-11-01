using Core.Enums;
using Core.Models.Business;

namespace Business.Executions
{
    /// <summary>
    /// Picks the debugger's hotkeys out of the keys a person presses. A combination is stored in the
    /// hook's names in the order it was pressed, "VcLeftControl+VcF9": its last key completes it,
    /// with exactly its keys down. A key held down repeats, and only its first press counts.
    /// </summary>
    public sealed class DebuggerHotkeyReader
    {
        private const char SEPARATOR = '+';

        private readonly Dictionary<DebuggerHotkeyEnum, string[]> _combinations = new Dictionary<DebuggerHotkeyEnum, string[]>();
        private readonly HashSet<string> _held = new HashSet<string>();
        private readonly Lock _lock = new Lock();


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>Binds a hotkey to a combination. An empty one unbinds it.</summary>
        public void Bind(DebuggerHotkeyEnum hotkey, string combination)
        {
            string[] keys = combination.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            lock (_lock)
            {
                if (keys.Length == 0)
                    _combinations.Remove(hotkey);
                else
                    _combinations[hotkey] = keys;
            }
        }

        /// <summary>Forgets what was held, for keys heard from now on.</summary>
        public void Reset()
        {
            lock (_lock)
            {
                _held.Clear();
            }
        }

        /// <summary>The hotkey this key completed, if any. Every key is followed, so what is held is known.</summary>
        public DebuggerHotkeyEnum? Read(RecordedInput input)
        {
            string? name = input.KeyChar;
            if (name == null)
                return null;

            lock (_lock)
            {
                if (input.Type == RecordedInputTypeEnum.KEY_UP)
                {
                    _held.Remove(name);
                    return null;
                }

                if (input.Type != RecordedInputTypeEnum.KEY_DOWN || !_held.Add(name))
                    return null;

                foreach (KeyValuePair<DebuggerHotkeyEnum, string[]> combination in _combinations)
                {
                    string[] keys = combination.Value;
                    if (keys[^1] == name && _held.SetEquals(keys))
                        return combination.Key;
                }

                return null;
            }
        }
    }
}
