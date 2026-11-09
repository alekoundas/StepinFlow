using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Draft.Actions
{
    /// <summary>
    /// A check marked while recording is a search: a wait until the element is there, or until it
    /// has gone, by its picture or by its words and the rule they follow, failing the execution when
    /// it times out. Every recorded check waits, so "it must be there" and "wait until it appears"
    /// are the one step. What comes after it goes in its Success branch, as after a click's search.
    /// With nothing to cut the picture from - the press kept no screenshot - it is no step.
    ///
    /// It can tell Discord, each way with a message of its own and the check's result: when it
    /// succeeds, first in its Success branch, and when it fails, before the execution ends.
    /// </summary>
    public static class CheckDraftBuilder
    {
        public static void Build(RecordedActionDto action, ReviewedActionDto review, RecordingDraft draft)
        {
            FlowStep? check;
            if (review.Choice == ReviewChoiceEnum.CHECK_GONE)
                check = draft.AddSearchFor(action, review, "Check", "Check icon", " gone", SearchModeEnum.WAIT_UNTIL_NOT_FOUND);
            else
                check = draft.AddSearchFor(action, review, "Check", "Check icon", string.Empty, SearchModeEnum.WAIT_UNTIL_FOUND);

            if (check == null || review.DiscordBotId == null)
                return;

            if (review.NotifyOnFailure)
                draft.AddToFailure(check, Notify(check, review.DiscordBotId.Value, review.FailureMessage));

            if (review.NotifyOnSuccess)
                draft.Add(Notify(check, review.DiscordBotId.Value, review.SuccessMessage));
        }

        private static FlowStep Notify(FlowStep check, int discordBotId, string? message)
        {
            return new FlowStep
            {
                FlowStepType = FlowStepTypeEnum.NOTIFY,
                Message = message?.Trim() ?? string.Empty,
                DiscordBotId = discordBotId,
                FlowStepReference = check,
            };
        }
    }
}
