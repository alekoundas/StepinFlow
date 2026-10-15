using Business.FlowScript.Models;
using Business.Recording.Draft.Actions;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Recording.Draft
{
    /// <summary>
    /// A reviewed recording into a flow, by a rule per kind of action rather than a question per
    /// action: a click waits for its element, moves onto it and clicks; typed text is an input; a
    /// key is a Press; a pause is no step, only the size of the next wait. One builder per kind, as
    /// there is a parser per kind of line.
    ///
    /// The draft is a flow on its own, written in the same rows a script reads into, so it prints
    /// and reads back like any file - which is what a model is later handed to shape.
    /// </summary>
    public sealed class RecordingDraftBuilder : IRecordingDraftBuilder
    {
        private readonly IScreenshotService _screenshotService;

        public RecordingDraftBuilder(IScreenshotService screenshotService)
        {
            _screenshotService = screenshotService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public FlowScriptSchema Build(
            FlowScriptSchema flow,
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt,
            FlowStep? under,
            int? index)
        {
            RecordingDraft draft = new RecordingDraft(flow, recording, screenAt, _screenshotService, under, index);
            Dictionary<int, ReviewedActionDto> reviews = reviewed.Actions.ToDictionary(x => x.ActionIndex);

            foreach (RecordedActionDto action in recording.Actions)
            {
                // A pause is not listed in Review, and what it measured is already the next action's wait.
                if (!reviews.TryGetValue(action.Index, out ReviewedActionDto? review) || review.Choice == ReviewChoiceEnum.LEAVE_OUT)
                    continue;

                switch (action.Kind)
                {
                    case RecordedActionKindEnum.CLICK:
                        ClickDraftBuilder.Build(action, review, draft);
                        break;

                    case RecordedActionKindEnum.DRAG:
                        DragDraftBuilder.Build(action, review, draft);
                        break;

                    case RecordedActionKindEnum.SCROLL:
                        ScrollDraftBuilder.Build(action, draft);
                        break;

                    case RecordedActionKindEnum.TYPING:
                        TypingDraftBuilder.Build(action, review, draft);
                        break;

                    case RecordedActionKindEnum.KEY_COMBINATION:
                    case RecordedActionKindEnum.KEY_HOLD:
                    case RecordedActionKindEnum.KEY_RELEASE:
                        KeyDraftBuilder.Build(action, draft);
                        break;
                }
            }

            return flow;
        }
    }
}
