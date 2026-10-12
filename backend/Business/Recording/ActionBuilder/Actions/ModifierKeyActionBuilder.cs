using Business.Recording.Models;
using Core.Catalogs;
using Core.Enums;
using Core.Enums.Business;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Recording.ActionBuilder.Actions
{
    /// <summary>
    /// A modifier is a KEY_HOLD and a KEY_RELEASE, with everything between them a step of its own
    /// ex: Ctrl+C = Hold Ctrl, Press C, Release Ctrl.
    ///
    /// A Shift held over typed text and nothing else never comes here: it is part of the text.
    /// </summary>
    public static class ModifierKeyActionBuilder
    {
        // ================================================================
        // Public methods
        // ================================================================

        public static void Build(RecordedInput down, RecordingReader reader)
        {
            List<RecordedInput> inputs = reader.GetKeyLongPressInputs(down);

            // Its key-up is left to the loop, to come where it came - after what it was held over.
            List<RecordedInput> downs = inputs.Where(x => x.Type == RecordedInputTypeEnum.KEY_DOWN).ToList();

            string name = NameOf(down);
            RecordedActionDto hold = new RecordedActionDto
            {
                Kind = RecordedActionKindEnum.KEY_HOLD,
                Summary = $"Held {name}",
                WindowTitle = down.WindowTitle,
                Text = name,
            };

            reader.AddHandled(new HumanAction(hold, downs));
        }

        public static void Release(RecordedInput up, RecordingReader reader)
        {
            string name = NameOf(up);

            RecordedActionDto release = new RecordedActionDto
            {
                Kind = RecordedActionKindEnum.KEY_RELEASE,
                Summary = $"Released {name}",
                WindowTitle = up.WindowTitle,
                Text = name,
            };

            reader.AddHandled(new HumanAction(release, [up]));
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string NameOf(RecordedInput input)
        {
            KeyCodeEnum key = input.KeyCode ?? throw new ArgumentException("Unknown Key.", nameof(input));
            return KeyCatalog.ModifierNames[key];
        }
    }
}
