using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Draft.Actions
{
    /// <summary>A scroll acts where the cursor is, like a click, so a move to where it was comes first.</summary>
    public static class ScrollDraftBuilder
    {
        public static void Build(RecordedActionDto action, RecordingDraft draft)
        {
            FlowPoint spot = draft.Point("Scroll spot", action, action.LocationX, action.LocationY);

            draft.Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowPoint = spot });
            draft.Add(new FlowStep
            {
                FlowStepType = FlowStepTypeEnum.CURSOR_SCROLL,
                CursorScrollDirectionType = action.ScrollDirection ?? CursorScrollDirectionTypeEnum.DOWN,
                LoopCount = Math.Max(1, action.ScrollAmount),
            });
        }
    }
}
