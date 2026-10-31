using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Draft.Actions
{
    /// <summary>
    /// A drag is one line, one gesture with both its ends on it: from the element waited for, or
    /// from where it started, to where it was let go.
    /// </summary>
    public static class DragDraftBuilder
    {
        public static void Build(RecordedActionDto action, ReviewedActionDto review, RecordingDraft draft)
        {
            FlowStep drag = new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_DRAG };

            FlowStep? search = null;
            if (review.Choice == ReviewChoiceEnum.WAIT_AND_CLICK)
                search = draft.AddSearchFor(action, review, "Find", "Find drag start", string.Empty, SearchModeEnum.WAIT_UNTIL_FOUND);

            if (search != null)
            {
                drag.FlowStepReference = search;
            }
            else
            {
                string text = draft.TextOf(action);
                string start = RecordingDraft.NameFrom("Drag", text, "Drag start");
                drag.FlowPoint = draft.Point(start, action, action.LocationX, action.LocationY);
            }

            drag.FlowPointEnd = draft.Point("Drop spot", action, action.LocationEndX, action.LocationEndY);
            draft.Add(drag);
        }
    }
}
