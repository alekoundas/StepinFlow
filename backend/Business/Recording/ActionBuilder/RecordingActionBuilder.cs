using Business.Recording.ActionBuilder.Actions;
using Core.Catalogs;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Recording.ActionBuilder
{
    /// <summary>
    /// Folds raw input into the things a person would say they did.
    /// 
    /// A press and a release are one click.
    /// 2 quick clicks are a double click.
    /// press-drag-release as a cursor drag.
    /// A scroll, with the notches follow the same direction is a cursor scroll step. 
    /// 
    /// A burst of typing is one entry (shift is ignored if used to make a char capital).
    /// A modifier is a KEY_HOLD and a KEY_RELEASE, with everything between them a step of its own
    /// (ex: Ctrl+C = Hold Ctrl, Press C, Release Ctrl).
    /// A long gap is a wait step.
    ///
    /// The KEY_TYPED RecordedInputs are droped from the list to handle, 
    /// also the TypedCharacter is assigned from the KEY_TYPED to KEY_DOWN.
    /// The KEY_TYPED cant be resolve the result typed character, so a compinion KEY_TYPED is added.
    /// 
    /// 
    /// Each action is read from its first input, and takes the rest of its own so the loop never reads them again. Nothing is skipped!
    /// What came between them is read as what it was, so the actions come out in the order they happened.
    /// </summary>
    public static class RecordingActionBuilder
    {
        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Convert raw recorded inputs to the actions a user did.
        /// </summary>
        public static List<RecordedActionDto> Build(IReadOnlyList<RecordedInput> inputs)
        {
            List<RecordedInput> readable = FilterAndUpdateRecordedInputs(inputs);
            RecordingReader reader = new RecordingReader(readable);

            while (reader.HasNext)
            {
                RecordedInput input = reader.GetNextRecordedInput();

                switch (input.Type)
                {
                    case RecordedInputTypeEnum.BUTTON_DOWN:
                        ClickActionBuilder.Build(input, reader);
                        break;

                    case RecordedInputTypeEnum.CURSOR_SCROLL:
                        ScrollActionBuilder.Build(input, reader);
                        break;

                    case RecordedInputTypeEnum.KEY_DOWN:
                        if (reader.IsTyped(input) || TypingActionBuilder.IsShiftOverText(input, reader)) // Text, and the Shift for its capitals.
                            TypingActionBuilder.Build(input, reader);

                        else if (KeyCatalog.IsModifier(input)) // Modifiers - ctrl, alt, shift, win
                            ModifierKeyActionBuilder.Build(input, reader);

                        else // Everything else ex: Enter, Tab, an arrow, an F-key
                            KeyPressActionBuilder.Build(input, reader);

                        break;

                    // Every other key-up is read with its key-down: this is a held modifier coming up.
                    // One with no key-down before it is nothing.
                    case RecordedInputTypeEnum.KEY_UP:
                        if (KeyCatalog.IsModifier(input))
                            ModifierKeyActionBuilder.Release(input, reader);
                        break;

                    // The time spent paused was the tester's, not a wait.
                    case RecordedInputTypeEnum.RESUMED:
                        reader.Resume();
                        break;
                }
            }

            return reader.Actions;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Remove the KEY_TYPED types from list and extract the TypedCharacter to assign on the KEY_DOWN.
        private static List<RecordedInput> FilterAndUpdateRecordedInputs(IReadOnlyList<RecordedInput> recordedInputs)
        {
            List<RecordedInput> readable = new List<RecordedInput>(recordedInputs.Count);
            RecordedInput? lastKeyDown = null;

            foreach (RecordedInput input in recordedInputs)
            {
                // Raised right after its key-down. A character outside the basic range arrives as two.
                if (input.Type == RecordedInputTypeEnum.KEY_TYPED)
                {
                    if (lastKeyDown != null && lastKeyDown.KeyCode == input.KeyCode)
                        lastKeyDown.TypedCharacter += input.TypedCharacter;

                    continue;
                }

                if (input.Type == RecordedInputTypeEnum.KEY_DOWN)
                    lastKeyDown = input;
                else
                    lastKeyDown = null;

                // Made from its KEY_TYPEDs alone, so building the same recording twice types the same text.
                if (lastKeyDown != null)
                    lastKeyDown.TypedCharacter = null;

                if (ExcludeLockKeys(input))
                    readable.Add(input);
            }

            return readable;
        }

        // Caps Lock and Num Lock are toggles, and what they changed is in the TypedCharacter.
        private static bool ExcludeLockKeys(RecordedInput input)
        {
            if (input.Type != RecordedInputTypeEnum.KEY_DOWN && input.Type != RecordedInputTypeEnum.KEY_UP)
                return true;

            return input.KeyCode != null && !KeyCatalog.LockKeys.Contains(input.KeyCode.Value);
        }
    }
}
