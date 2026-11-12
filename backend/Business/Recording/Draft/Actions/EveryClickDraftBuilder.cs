using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Draft.Actions
{
    /// <summary>
    /// Clicks on one picture in several places, one after another - the download icon on four
    /// rows - are one search that waits for the picture, then a Find All Images whose Success
    /// branch moves onto each hit and clicks it. Every one on the screen is clicked, as many as
    /// there are then, which is what doing the same thing on every row means.
    /// </summary>
    public static class EveryClickDraftBuilder
    {
        public static void Build(IReadOnlyList<RecordedActionDto> clicks, ReviewedActionDto review, RecordingDraft draft)
        {
            RecordedActionDto first = clicks[0];

            FlowStep? search = draft.AddSearchFor(first, review, "Find", "Find icon", string.Empty, SearchModeEnum.WAIT_UNTIL_FOUND);
            FlowStepTemplate? template = draft.Template(first, review);
            if (search == null || template == null)
            {
                foreach (RecordedActionDto each in clicks)
                    ClickDraftBuilder.Build(each, review, draft);

                return;
            }

            string text = draft.TextOf(first);
            FlowStep every = new FlowStep
            {
                FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE,
                Name = draft.Name(RecordingDraft.NameFrom("Find every", text, "Find every icon")),
                SearchMode = SearchModeEnum.FIND_ALL,
                FlowArea = draft.MainArea,
                FlowStepTemplates = new List<FlowStepTemplate> { template },
                CodeComment = $"clicked in {clicks.Count} places while recording",
            };

            FlowStep move = new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowStepReference = every };
            FlowStep click = new FlowStep
            {
                FlowStepType = FlowStepTypeEnum.CURSOR_CLICK,
                CursorButtonType = first.CursorButtonType ?? CursorButtonTypeEnum.LEFT_BUTTON,
                CursorButtonActionType = first.CursorButtonActionType ?? CursorButtonActionTypeEnum.SINGLE_CLICK,
            };

            draft.AddEvery(every, [move, click]);
        }
    }
}
