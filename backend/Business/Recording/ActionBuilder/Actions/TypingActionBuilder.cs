using System.Text;

using Business.Recording.Models;
using Core.Catalogs;
using Core.Enums;
using Core.Enums.Business;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Recording.ActionBuilder.Actions
{
    /// <summary>
    /// Convert recorded actions to human actions of: 
    /// 
    /// A key held down types its repeats, as it did on screen.
    /// A burst of typing to one TYPING entry (reads all uninterrupted characters). 
    /// A Backspace is NOT a step and actually takes back what the user typed (if  no text left, it ignores everything).
    /// 
    /// Each key carries the character writen on the screen so the Shift, Caps-Lock and Alt are ignored.
    /// A Shift held over text and nothing else is part of the entry, not a step: "Hello" is one line.
    ///
    /// Stops being uninterrupted when: a click, a modifier key, another window stole focus, a resume. 
    /// Time does not!.
    /// </summary>
    public static class TypingActionBuilder
    {
        // ================================================================
        // Public methods
        // ================================================================

        public static void Build(RecordedInput first, RecordingReader reader)
        {
            List<RecordedInput> inputs = new List<RecordedInput>();
            StringBuilder text = new StringBuilder();

            // The entry starts at a key that typed text, or at the Shift of its first capital.
            if (KeyCatalog.IsModifier(first, KeyCatalog.SHIFT))
                ExtractShift(first, reader, inputs);
            else
                text.Append(ExtractText(first, reader, inputs));

            foreach (RecordedInput next in reader.After(first))
            {
                // A click, or an app stole the focus  -  Typing interrupted!
                if (next.Type != RecordedInputTypeEnum.KEY_DOWN || next.WindowHandle != first.WindowHandle)
                    break;

                // Handle a Shift over text is in TypedCharacter as a capital char.
                if (IsShiftOverText(next, reader))
                    ExtractShift(next, reader, inputs);

                // Handle text.
                else if (KeyCatalog.IsText(next.TypedCharacter))
                    text.Append(ExtractText(next, reader, inputs));

                // Handle deletions
                else if (next.KeyCode != KeyCodeEnum.Backspace || !Erase(next, reader, inputs, text))
                    break;
            }

            // The Shift went to one window and its text to another: nothing was typed here.
            if (text.Length == 0)
                return;

            RecordedActionDto typing = new RecordedActionDto
            {
                Kind = RecordedActionKindEnum.TYPING,
                Summary = $"Typed \"{text}\"",
                Text = text.ToString(),
            };

            List<RecordedInput> ordered = reader.InOrder(inputs); // Times can tie. Use the recordinng order.
            reader.AddHandled(new HumanAction(typing, ordered));
        }

        /// <summary>
        /// Whether the key is a Shift held over typed text.
        /// </summary>
        public static bool IsShiftOverText(RecordedInput down, RecordingReader reader)
        {
            if (!KeyCatalog.IsModifier(down, KeyCatalog.SHIFT))
                return false;

            bool hasText = false;

            foreach (RecordedInput input in reader.After(down))
            {
                // Its own repeats, until its key-up ends what it was held over.
                if (input.KeyCode == down.KeyCode)
                {
                    if (input.Type == RecordedInputTypeEnum.KEY_UP)
                        break;

                    continue;
                }

                // The keys it was held over coming up.
                if (input.Type == RecordedInputTypeEnum.KEY_UP)
                    continue;

                // A click, a scroll, a key that typed nothing or typed under a held Ctrl.
                if (input.Type != RecordedInputTypeEnum.KEY_DOWN || !reader.IsTyped(input))
                    return false;

                hasText = true;
            }

            return hasText;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // A Shift is part of the entry it raised the capitals of, and nothing of its own.
        private static void ExtractShift(RecordedInput down, RecordingReader reader, List<RecordedInput> inputs)
        {
            List<RecordedInput> shift = reader.GetKeyLongPressInputs(down);
            reader.MarkAsHandled(shift); // The loop skips those.
            inputs.AddRange(shift);
        }

        // Every key-down typed its character, the ones the keyboard repeated included.
        private static string ExtractText(RecordedInput down, RecordingReader reader, List<RecordedInput> inputs)
        {
            List<RecordedInput> key = reader.GetKeyLongPressInputs(down);
            reader.MarkAsHandled(key); // The loop skips those.
            inputs.AddRange(key);

            List<string> textList = key
                .Where(x => x.Type == RecordedInputTypeEnum.KEY_DOWN)
                .Select(x => x.TypedCharacter ?? "")
                .ToList();

            return string.Concat(textList);
        }

        // A Backspace takes back a character for every time it went down.
        // Erasing the whole text ignores all the recorded inputs.
        private static bool Erase(RecordedInput down, RecordingReader reader, List<RecordedInput> inputs, StringBuilder text)
        {
            List<RecordedInput> key = reader.GetKeyLongPressInputs(down);
            int erasedCount = key.Count(x => x.Type == RecordedInputTypeEnum.KEY_DOWN);
            if (erasedCount >= text.Length)
                return false;

            reader.MarkAsHandled(key); // The loop skips those.
            inputs.AddRange(key);
            text.Length -= erasedCount; // Erase text.

            return true;
        }
    }
}
