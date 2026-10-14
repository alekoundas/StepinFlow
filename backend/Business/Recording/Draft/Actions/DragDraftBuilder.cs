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

            FlowStepTemplate? template = null;
            if (review.Choice == ReviewChoiceEnum.WAIT_AND_CLICK)
                template = draft.Template(action, review);

            string text = draft.ElementOf(action)?.Text ?? string.Empty;

            if (template != null)
            {
                string name = draft.Name(RecordingDraft.NameFrom("Find", text, "Find drag start"));
                drag.FlowStepReference = draft.AddSearch(action, name, template);
            }
            else
            {
                string start = RecordingDraft.NameFrom("Drag", text, "Drag start");
                drag.FlowPoint = draft.Point(start, action, action.LocationX, action.LocationY);
            }

            drag.FlowPointEnd = draft.Point("Drop spot", action, action.LocationEndX, action.LocationEndY);
            draft.Add(drag);
        }
    }
}
