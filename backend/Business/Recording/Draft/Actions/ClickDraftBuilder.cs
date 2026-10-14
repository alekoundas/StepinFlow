using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Draft.Actions
{
    /// <summary>
    /// A click is three lines, because a click acts where the cursor is: a wait for the element, a
    /// move onto wherever it was found, the click. At its exact spot, a move to a point and the click.
    /// </summary>
    public static class ClickDraftBuilder
    {
        public static void Build(RecordedActionDto action, ReviewedActionDto review, RecordingDraft draft)
        {
            FlowStep move = new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE };

            FlowStepTemplate? template = null;
            if (review.Choice == ReviewChoiceEnum.WAIT_AND_CLICK)
                template = draft.Template(action, review);

            string text = draft.ElementOf(action)?.Text ?? string.Empty;

            if (template != null)
            {
                string name = draft.Name(RecordingDraft.NameFrom("Find", text, "Find icon"));
                move.FlowStepReference = draft.AddSearch(action, name, template);
            }
            else
            {
                string spot = RecordingDraft.NameFrom("Click", text, "Click spot");
                move.FlowPoint = draft.Point(spot, action, action.LocationX, action.LocationY);
            }

            draft.Add(move);
            draft.Add(new FlowStep
            {
                FlowStepType = FlowStepTypeEnum.CURSOR_CLICK,
                CursorButtonType = action.CursorButtonType ?? CursorButtonTypeEnum.LEFT_BUTTON,
                CursorButtonActionType = action.CursorButtonActionType ?? CursorButtonActionTypeEnum.SINGLE_CLICK,
            });
        }
    }
}
