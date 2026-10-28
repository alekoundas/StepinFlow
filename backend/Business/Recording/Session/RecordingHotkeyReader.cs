using Core.Enums;
using Core.Models.Business;

namespace Business.Recording.Session
{
    /// <summary>
    /// Picks the recording bar's hotkeys out of the input at the hook. A hotkey pauses, resumes or
    /// stops whatever window is in front, and none of its keys is recorded.
    ///
    /// A combination is stored in the hook's names in the order it was pressed,
    /// "VcLeftControl+VcF9", so its last key completes it. The keys before that one are held back
    /// while they are down: when the combination follows they go with it, and anything else lets
    /// them through first, in order. Otherwise the Ctrl of Ctrl+F9 would already be recorded by
    /// the time F9 came. A key held down repeats, and only its first press counts.
    ///
    /// A recording gesture's modifiers are held back the same way. The hook decides which press is
    /// a gesture, and marks it; the modifiers held for it are left out with it.
    /// </summary>
    public sealed class RecordingHotkeyReader
    {
        private const char SEPARATOR = '+';

        private readonly Dictionary<RecordingHotkeyEnum, string[]> _combinations = new Dictionary<RecordingHotkeyEnum, string[]>();
        private readonly HashSet<string> _gestureModifiers = new HashSet<string>();     // Ctrl, Alt, Shift, Win
        private readonly HashSet<string> _held = new HashSet<string>();         // Every key down now.
        private readonly HashSet<string> _swallowed = new HashSet<string>();    // A pressed hotkey's keys, left out until they come up.
        private readonly List<RecordedInput> _heldBack = new List<RecordedInput>();
        private readonly Lock _lock = new Lock();

        private bool _isLastKeyDownSwallowed;


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>Binds a hotkey to a combination. An empty one unbinds it.</summary>
        public void Bind(RecordingHotkeyEnum hotkey, string combination)
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

        /// <summary>The gestures whose modifiers to hold back until a press shows whether one was made.</summary>
        public void BindGestures(IReadOnlyList<PressGesture> gestures)
        {
            lock (_lock)
            {
                _gestureModifiers.Clear();
                _gestureModifiers.UnionWith(gestures.SelectMany(x => x.Modifiers));
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _held.Clear();
                _swallowed.Clear();
                _heldBack.Clear();
                _isLastKeyDownSwallowed = false;
            }
        }

        /// <summary>
        /// What to record now, in order: nothing while a combination's first keys are held back,
        /// then those and this input once it is not the combination. hotkey is the one this input
        /// completed, if any.
        /// </summary>
        public IReadOnlyList<RecordedInput> Read(RecordedInput input, out RecordingHotkeyEnum? hotkey)
        {
            hotkey = null;

            lock (_lock)
            {
                switch (input.Type)
                {
                    case RecordedInputTypeEnum.KEY_DOWN:
                        return ReadKeyDown(input, out hotkey);

                    // What a key typed is raised right after its key-down, and goes with it.
                    case RecordedInputTypeEnum.KEY_TYPED:
                        if (_isLastKeyDownSwallowed)
                            return [];
                        break;

                    case RecordedInputTypeEnum.KEY_UP:
                        if (input.KeyChar != null)
                        {
                            _held.Remove(input.KeyChar);
                            if (_swallowed.Remove(input.KeyChar))
                                return [];
                        }
                        break;

                    case RecordedInputTypeEnum.BUTTON_DOWN:
                        if (input.Gesture != null)
                            SwallowGestureModifiers();
                        break;
                }

                return Release(input);
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private IReadOnlyList<RecordedInput> ReadKeyDown(RecordedInput down, out RecordingHotkeyEnum? hotkey)
        {
            hotkey = null;
            _isLastKeyDownSwallowed = false;

            string? name = down.KeyChar;
            if (name == null)
                return Release(down);

            bool isRepeat = !_held.Add(name);

            if (_swallowed.Contains(name))
            {
                _isLastKeyDownSwallowed = true;
                return [];
            }

            if (!isRepeat)
                hotkey = Match(name);

            if (hotkey != null)
            {
                Swallow(name, _combinations[hotkey.Value]);
                return Release(null);
            }

            // A key that may start a combination or a gesture waits to see what follows, and so do its repeats.
            bool isHeldBack = _heldBack.Any(x => x.KeyChar == name);
            if (isHeldBack || (!isRepeat && (IsFirstKeys(name) || IsGestureModifier(name))))
            {
                _heldBack.Add(down);
                return [];
            }

            return Release(down);
        }

        // Exactly the combination is down, and this key completed it.
        private RecordingHotkeyEnum? Match(string name)
        {
            foreach (KeyValuePair<RecordingHotkeyEnum, string[]> combination in _combinations)
            {
                string[] keys = combination.Value;
                if (keys[^1] == name && _held.SetEquals(keys))
                    return combination.Key;
            }

            return null;
        }

        private bool IsFirstKeys(string name)
        {
            foreach (string[] keys in _combinations.Values)
            {
                int last = keys.Length - 1;
                if (Array.IndexOf(keys, name, 0, last) >= 0)
                    return true;
            }

            return false;
        }

        private bool IsGestureModifier(string name)
        {
            string? modifier = PressGesture.ModifierOf(name);
            return modifier != null && _gestureModifiers.Contains(modifier);
        }

        // The modifiers held back for a gesture's press. One already recorded keeps its key-up, as
        // for a hotkey.
        private void SwallowGestureModifiers()
        {
            foreach (RecordedInput heldBack in _heldBack.Where(x => x.KeyChar != null && PressGesture.ModifierOf(x.KeyChar) != null))
                _swallowed.Add(heldBack.KeyChar!);

            _heldBack.RemoveAll(x => x.KeyChar != null && PressGesture.ModifierOf(x.KeyChar) != null);
        }

        // The key that completed it, and the ones held back for it. One already recorded keeps its
        // key-up, or the recording would hold it down for good.
        private void Swallow(string name, string[] keys)
        {
            _isLastKeyDownSwallowed = true;
            _swallowed.Add(name);

            foreach (RecordedInput heldBack in _heldBack)
            {
                if (heldBack.KeyChar != null && keys.Contains(heldBack.KeyChar))
                    _swallowed.Add(heldBack.KeyChar);
            }

            _heldBack.RemoveAll(x => x.KeyChar != null && keys.Contains(x.KeyChar));
        }

        // What was held back, then this input.
        private List<RecordedInput> Release(RecordedInput? input)
        {
            List<RecordedInput> released = new List<RecordedInput>(_heldBack);
            _heldBack.Clear();

            if (input != null)
                released.Add(input);

            return released;
        }
    }
}
