using Business.Recording.Models;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Recording.ActionBuilder.Actions
{
    /// <summary>
    /// Convert recorded actions to human actions of: 
    /// 
    /// A scroll, with the notches be in the same direction is one cursor scroll step. 
    /// </summary>
    public static class ScrollActionBuilder
    {
        public static void Build(RecordedInput first, RecordingReader reader)
        {
            List<RecordedInput> turns = [first];
            int rotation = first.ScrollRotation;

            foreach (RecordedInput next in reader.After(first).Where(x => !IsKey(x)))
            {
                if (!IsSameScroll(first, next))
                    break;

                turns.Add(next);
                rotation += next.ScrollRotation;
            }


            int notches = Notches(rotation, first.ScrollUnitsPerNotch);
            RecordedActionDto scroll = Scroll(first, notches);
            reader.AddHandled(new HumanAction(scroll, turns));
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static bool IsKey(RecordedInput input)
        {
            return input.Type == RecordedInputTypeEnum.KEY_DOWN
                || input.Type == RecordedInputTypeEnum.KEY_UP
                || input.Type == RecordedInputTypeEnum.KEY_TYPED;
        }

        // Added up before dividing, so a touchpad's slices make whole notches rather than one each.
        private static int Notches(int rotation, int unitsPerNotch)
        {
            double notches = (double)rotation / Math.Max(unitsPerNotch, 1);
            int whole = (int)Math.Round(notches);

            return Math.Max(1, whole);
        }

        private static bool IsSameScroll(RecordedInput first, RecordedInput next)
        {
            return next.Type == RecordedInputTypeEnum.CURSOR_SCROLL && next.ScrollDirection == first.ScrollDirection;
        }

        private static RecordedActionDto Scroll(RecordedInput first, int amount)
        {
            return new RecordedActionDto
            {
                Kind = RecordedActionKindEnum.SCROLL,
                Summary = $"Scrolled {first.ScrollDirection.ToString()?.ToLowerInvariant()} {amount}",
                WindowTitle = first.WindowTitle,
                Window = first.Window,
                LocationX = first.PhysicalX,
                LocationY = first.PhysicalY,
                ScrollDirection = first.ScrollDirection,
                ScrollAmount = amount,
            };
        }
    }
}
