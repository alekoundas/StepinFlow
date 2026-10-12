using System.Globalization;

using Business.Recording.Models;
using Core.Enums;
using Core.Enums.Business;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Recording.ActionBuilder.Actions
{
    /// <summary>
    /// Handle any leftover keys: Enter, Tab, an arrow, an F-key, etc.
    /// </summary>
    public static class KeyPressActionBuilder
    {
        // ================================================================
        // Public methods
        // ================================================================

        public static void Build(RecordedInput down, RecordingReader reader)
        {
            List<RecordedInput> inputs = reader.GetKeyLongPressInputs(down);

            RecordedActionDto press = Pressed(inputs);
            reader.AddHandled(new HumanAction(press, inputs));
        }


        // ================================================================
        // Private methods
        // ================================================================

        // One press is what the tester did, and the repeats are part of it: counted, never replayed.
        private static RecordedActionDto Pressed(IReadOnlyList<RecordedInput> inputs)
        {
            RecordedInput down = inputs[0];
            RecordedInput last = inputs[^1];
            KeyCodeEnum key = down.KeyCode ?? throw new ArgumentException("Unknown Key.", nameof(inputs));

            RecordedActionDto press = new RecordedActionDto
            {
                Kind = RecordedActionKindEnum.KEY_COMBINATION,
                WindowTitle = down.WindowTitle,
                Text = key.ToString(),
                RepeatCount = inputs.Count(x => x.Type == RecordedInputTypeEnum.KEY_DOWN) - 1,
            };

            // Log how long was held.
            if (last.Type == RecordedInputTypeEnum.KEY_UP)
            {
                TimeSpan heldFor = last.CreatedOn - down.CreatedOn;
                press.HoldMilliseconds = (int)Math.Round(heldFor.TotalMilliseconds);
            }

            press.Summary = $"Pressed {press.Text}";
            if (press.RepeatCount > 0)
            {
                double seconds = Math.Round(press.HoldMilliseconds / 1000d, 1);
                press.Summary += $", held {seconds.ToString(CultureInfo.InvariantCulture)}s";
            }

            return press;
        }
    }
}
