using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Draft.Actions
{
    /// <summary>
    /// A key pressed is a Press line. A key held over a click is its own line before and after it,
    /// hold and release, as it was recorded.
    /// </summary>
    public static class KeyDraftBuilder
    {
        public static void Build(RecordedActionDto action, RecordingDraft draft)
        {
            KeyboardKeyActionTypeEnum keyAction = KeyboardKeyActionTypeEnum.PRESS;
            if (action.Kind == RecordedActionKindEnum.KEY_HOLD)
                keyAction = KeyboardKeyActionTypeEnum.HOLD;
            if (action.Kind == RecordedActionKindEnum.KEY_RELEASE)
                keyAction = KeyboardKeyActionTypeEnum.RELEASE;

            draft.Add(new FlowStep
            {
                FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT,
                KeyboardInputType = KeyboardInputTypeEnum.COMBINATION,
                KeyboardInputText = action.Text ?? string.Empty,
                KeyboardKeyActionType = keyAction,
            });
        }
    }
}
