using Business.Recording.Models;
using Core.Catalogs;
using Core.Enums;
using Core.Enums.Business;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Recording.ActionBuilder
{
    /// <summary>
    /// The recorded inputs in order, each read once, and the actions they become in the order the builders add them. 
    /// What the builders need to know about the recording around an input is asked here.
    /// </summary>
    public sealed class RecordingReader
    {
        private readonly RecordedInput[] _recordedInputs;                                               // All the recorded inputs, in order.
        private readonly HashSet<RecordedInput> _handledRecordedInputs = new HashSet<RecordedInput>();  // Handled/Completed recorded inputs.
        private readonly ActionTimeline _timeline = new ActionTimeline();                               // What the inputs became
        private int _currentIndex;


        /// <summary> See if there are more unhandled recorded inputs. </summary>
        public bool HasNext { get { return _currentIndex < _recordedInputs.Length; } }

        /// <summary> The actions added so far, numbered, with the pauses between them. </summary>
        public List<RecordedActionDto> Actions { get { return _timeline.Actions; } }


        public RecordingReader(IReadOnlyList<RecordedInput> inputs)
        {
            _recordedInputs = inputs.ToArray();
        }

        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Get next unhandled RecordedInput.
        /// </summary>
        public RecordedInput GetNextRecordedInput()
        {
            RecordedInput input = _recordedInputs[_currentIndex];
            _currentIndex++;
            IncreaseIndex();

            return input;
        }

        /// <summary>
        /// Marks these inputs as handled, so the loop skips them.
        /// </summary>
        public void MarkAsHandled(IEnumerable<RecordedInput> inputs)
        {
            foreach (RecordedInput input in inputs)
                _handledRecordedInputs.Add(input);

            IncreaseIndex();
        }

        /// <summary>
        /// Get the RecordedInputs after this one not yet taken by an action, nearest first.
        /// </summary>
        public IEnumerable<RecordedInput> After(RecordedInput input)
        {
            int inputIndex = Array.IndexOf(_recordedInputs, input);
            for (int index = inputIndex + 1; index < _recordedInputs.Length; index++)
            {
                if (!_handledRecordedInputs.Contains(_recordedInputs[index]))
                    yield return _recordedInputs[index];
            }
        }

        /// <summary>
        /// These inputs in the order they were recorded. Times can tie, the order the hook raised them cannot.
        /// </summary>
        public List<RecordedInput> InOrder(IEnumerable<RecordedInput> inputs)
        {
            return inputs.OrderBy(x => Array.IndexOf(_recordedInputs, x)).ToList();
        }


        /// <summary>
        /// Add the genrated human actionn to the timeline and mark it as handled.
        /// </summary>
        public void AddHandled(HumanAction humanAction)
        {
            MarkAsHandled(humanAction.Inputs);
            _timeline.Add(humanAction);
        }

        /// <summary> A mark the tester made, which took no time: the wait before the next action runs from the one before it. </summary>
        public void AddMark(HumanAction mark)
        {
            MarkAsHandled(mark.Inputs);
            _timeline.AddMark(mark);
        }

        /// <summary> Recording paused and then resumed. The time paused was theirs, not a wait. </summary>
        public void Resume()
        {
            _timeline.Resume();
        }

        /// <summary>
        /// The key-downs the keyboard repeats while the key is held, and its key-up.
        /// </summary>
        public List<RecordedInput> GetKeyLongPressInputs(RecordedInput down)
        {
            List<RecordedInput> inputs = [down];

            foreach (RecordedInput next in After(down).Where(x => x.KeyCode == down.KeyCode))
            {
                inputs.Add(next);

                if (next.Type == RecordedInputTypeEnum.KEY_UP)
                    break;
            }

            return inputs;
        }

        /// <summary>
        /// Get the characters raised right after press
        /// (typing is different recording input and a coppy of the press - not all keys able to emit this - only text).
        /// </summary>
        public string? GetTypedCharacter(RecordedInput down)
        {
            List<string?> typed = After(down)
                .TakeWhile(x => x.Type == RecordedInputTypeEnum.KEY_TYPED && x.KeyCode == down.KeyCode) // yeild 
                .Select(x => x.TypedCharacter)
                .ToList();

            if (typed.Count == 0)
                return null;

            return string.Concat(typed);
        }

        /// <summary>
        /// Whether the key typed a character with no Ctrl, Alt or Win held over it.
        /// </summary>
        public bool IsTyped(RecordedInput down)
        {
            string? typedCharacter = GetTypedCharacter(down);
            if (!KeyCatalog.IsText(typedCharacter))
                return false;

            // See from that RecordedInput and back if a Ctrl, Alt or Win went down and has not come up.
            HashSet<KeyCodeEnum> releasedModifiers = new HashSet<KeyCodeEnum>();

            int downIndex = Array.IndexOf(_recordedInputs, down);
            for (int index = downIndex - 1; index >= 0; index--)
            {
                RecordedInput input = _recordedInputs[index];
                if (input.KeyCode == null || !KeyCatalog.IsModifier(input) || KeyCatalog.IsModifier(input, KeyCatalog.SHIFT))
                    continue;

                // Add the released modifiers.
                if (input.Type == RecordedInputTypeEnum.KEY_UP)
                    releasedModifiers.Add(input.KeyCode.Value);

                // Check if modifier was released.
                else if (!releasedModifiers.Contains(input.KeyCode.Value))
                    return false;
            }

            return true;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private void IncreaseIndex()
        {
            while (_currentIndex < _recordedInputs.Length && _handledRecordedInputs.Contains(_recordedInputs[_currentIndex]))
                _currentIndex++;
        }
    }
}
