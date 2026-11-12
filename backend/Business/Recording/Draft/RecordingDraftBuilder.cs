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
    /// action: a click waits for its element, moves onto it and clicks; clicks on one picture in
    /// several places, one after another, are one Find All Images; typed text is an input; a key is
    /// a Press; a pause is no step, only the size of the next wait; a stage the tester named is a
    /// stage marker over the next search. One builder per kind, as there is a parser per kind of
    /// line.
    ///
    /// The draft is a flow on its own, written in the same rows a script reads into, so it prints
    /// and reads back like any file.
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

        public RecordingDraft Build(
            FlowScriptSchema flow,
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt,
            FlowStep? under,
            int? index)
        {
            RecordingDraft draft = new RecordingDraft(flow, recording, screenAt, _screenshotService, under, index);
            Dictionary<int, ReviewedActionDto> reviews = reviewed.Actions.ToDictionary(x => x.ActionIndex);
            HashSet<int> folded = new HashSet<int>();

            foreach (RecordedActionDto action in recording.Actions)
            {
                // A pause is not listed in Review, and what it measured is already the next action's wait.
                if (!reviews.TryGetValue(action.Index, out ReviewedActionDto? review) || folded.Contains(action.Index))
                    continue;

                bool isLeftOut = review.Choice == ReviewChoiceEnum.LEAVE_OUT;

                // A stage begins at the next search, so an action left out still says where one
                // does. A mark of its own left out, or never named, is no stage.
                string stage = review.StageName?.Trim() ?? string.Empty;
                if (stage.Length > 0 && !(action.Kind == RecordedActionKindEnum.STAGE && isLeftOut))
                    draft.BeginStage(stage);

                if (isLeftOut)
                    continue;

                draft.StartAction(action);

                List<RecordedActionDto> clicks = ClicksOnOnePicture(action, recording, reviews);
                if (clicks.Count > 1)
                {
                    EveryClickDraftBuilder.Build(clicks, review, draft);
                    folded.UnionWith(clicks.Select(x => x.Index));
                    continue;
                }

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

                    case RecordedActionKindEnum.CHECK:
                        CheckDraftBuilder.Build(action, review, draft);
                        break;

                    case RecordedActionKindEnum.SUBSTITUTE:
                        SubstituteDraftBuilder.Build(review, draft);
                        break;
                }
            }

            draft.AddStages();
            return draft;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The click, and the clicks straight after it on the same picture in other places, each
        // waited for by its picture and clicked the same way, with nothing else kept between them
        // and no stage beginning among them: the same thing done in several places.
        private static List<RecordedActionDto> ClicksOnOnePicture(RecordedActionDto first, RecordingResultDto recording, Dictionary<int, ReviewedActionDto> reviews)
        {
            List<RecordedActionDto> clicks = [first];
            ClickGroupDto? group = recording.ClickGroups.FirstOrDefault(x => x.IsMoving && x.ActionIndexes.Contains(first.Index));
            if (group == null || !IsClickByPicture(first, reviews[first.Index]))
                return clicks;

            foreach (RecordedActionDto next in recording.Actions.Where(x => x.Index > first.Index))
            {
                if (!reviews.TryGetValue(next.Index, out ReviewedActionDto? review))
                    continue;

                if (review.Choice == ReviewChoiceEnum.LEAVE_OUT && string.IsNullOrWhiteSpace(review.StageName))
                    continue;

                bool isSame = group.ActionIndexes.Contains(next.Index)
                    && IsClickByPicture(next, review)
                    && string.IsNullOrWhiteSpace(review.StageName)
                    && next.CursorButtonType == first.CursorButtonType
                    && next.CursorButtonActionType == first.CursorButtonActionType;

                if (!isSame)
                    break;

                clicks.Add(next);
            }

            return clicks;
        }

        private static bool IsClickByPicture(RecordedActionDto action, ReviewedActionDto review)
        {
            bool isByWords = review.SearchBy == SearchByEnum.TEXT && !string.IsNullOrWhiteSpace(review.SearchText);
            return action.Kind == RecordedActionKindEnum.CLICK && review.Choice == ReviewChoiceEnum.WAIT_AND_CLICK && !isByWords;
        }
    }
}
