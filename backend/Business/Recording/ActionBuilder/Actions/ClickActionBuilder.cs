using Business.Recording.Models;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Recording.ActionBuilder.Actions
{
    /// <summary>
    /// Convert recorded actions to human actions of: 
    /// 
    /// Press and release click as SINGLE_CLICK,
    /// 2 quick press and releases as DOUBLE_CLICK,
    /// press-drag-releases as CURSOR_DRAG
    /// </summary>
    public static class ClickActionBuilder
    {
        // Constants
        private const int DRAG_THRESHOLD_PIXELS = 5;                // A press and release at different points is a drag above that threshold.
        private const int DOUBLE_CLICK_THRESHOLD_PIXELS = 6;        // A click and a click a bit further away is a single click above that threshold.
        private static readonly TimeSpan DOUBLE_CLICK_WINDOW = TimeSpan.FromMilliseconds(500); // Two presses are two clicks above that threshold.


        // ================================================================
        // Public methods
        // ================================================================

        public static void Build(RecordedInput press, RecordingReader reader)
        {
            List<RecordedInput> inputs = [press];

            // Find next cursor btn release.
            RecordedInput? release = ReleaseOf(press, reader);
            if (release != null)
                inputs.Add(release);

            // If none release exists, copy the press event as relese. ex: User paused while pressed click and on resume its already released.
            RecordedInput end = release ?? press;

            // Add Drag.
            if (IsDrag(press, end))
            {
                RecordedActionDto drag = Drag(press, end);
                reader.AddHandled(new HumanAction(drag, inputs));
                return;
            }

            // Add double click.
            RecordedInput? secondPress = SecondPressOf(press, end, reader);
            if (secondPress != null)
            {
                inputs.Add(secondPress);

                RecordedInput? secondRelease = ReleaseOf(secondPress, reader);
                if (secondRelease != null)
                    inputs.Add(secondRelease);

                RecordedActionDto doubleClick = Click(press, CursorButtonActionTypeEnum.DOUBLE_CLICK);
                reader.AddHandled(new HumanAction(doubleClick, inputs));
                return;
            }

            // Add single click.
            RecordedActionDto click = Click(press, CursorButtonActionTypeEnum.SINGLE_CLICK);
            reader.AddHandled(new HumanAction(click, inputs));
        }


        // ================================================================
        // Private methods
        // ================================================================
        private static bool IsDrag(RecordedInput press, RecordedInput end)
        {
            return Math.Abs(end.PhysicalX - press.PhysicalX) > DRAG_THRESHOLD_PIXELS
                || Math.Abs(end.PhysicalY - press.PhysicalY) > DRAG_THRESHOLD_PIXELS;
        }

        // Get the next release of the same button.
        private static RecordedInput? ReleaseOf(RecordedInput press, RecordingReader reader)
        {
            return reader.After(press).FirstOrDefault(x => x.Type == RecordedInputTypeEnum.BUTTON_UP && x.CursorButtonType == press.CursorButtonType);
        }

        // Get the next press of the same btn.
        private static RecordedInput? SecondPressOf(RecordedInput press, RecordedInput release, RecordingReader reader)
        {
            // Find the next press of the same btn.
            RecordedInput? nextPress = reader
                .After(release)
                .Where(x => x.CursorButtonType == press.CursorButtonType)
                .FirstOrDefault(x => x.Type == RecordedInputTypeEnum.BUTTON_DOWN);

            if (nextPress == null)
                return null;

            // Compare the time and pixels between clicks.
            bool isSecondPress =
                (nextPress.CreatedOn - press.CreatedOn) <= DOUBLE_CLICK_WINDOW &&
                Math.Abs(nextPress.PhysicalX - press.PhysicalX) <= DOUBLE_CLICK_THRESHOLD_PIXELS &&
                Math.Abs(nextPress.PhysicalY - press.PhysicalY) <= DOUBLE_CLICK_THRESHOLD_PIXELS;

            if (!isSecondPress)
                return null;

            return nextPress;
        }

        private static RecordedActionDto Click(RecordedInput press, CursorButtonActionTypeEnum buttonAction)
        {
            string verb = "Clicked";
            if (buttonAction == CursorButtonActionTypeEnum.DOUBLE_CLICK)
                verb = "Double-clicked";

            return new RecordedActionDto
            {
                Kind = RecordedActionKindEnum.CLICK,
                Summary = $"{verb} at {press.PhysicalX}, {press.PhysicalY}",
                WindowTitle = press.WindowTitle,
                Window = press.Window,
                ScreenshotIndex = press.HasScreenshot ? press.Index : null,
                LocationX = press.PhysicalX,
                LocationY = press.PhysicalY,
                CursorButtonType = press.CursorButtonType,
                CursorButtonActionType = buttonAction,
            };
        }

        private static RecordedActionDto Drag(RecordedInput press, RecordedInput release)
        {
            return new RecordedActionDto
            {
                Kind = RecordedActionKindEnum.DRAG,
                Summary = $"Dragged from {press.PhysicalX}, {press.PhysicalY} to {release.PhysicalX}, {release.PhysicalY}",
                WindowTitle = press.WindowTitle,
                Window = press.Window,
                ScreenshotIndex = press.HasScreenshot ? press.Index : null,
                LocationX = press.PhysicalX,
                LocationY = press.PhysicalY,
                LocationEndX = release.PhysicalX,
                LocationEndY = release.PhysicalY,
                CursorButtonType = press.CursorButtonType,
            };
        }
    }
}
