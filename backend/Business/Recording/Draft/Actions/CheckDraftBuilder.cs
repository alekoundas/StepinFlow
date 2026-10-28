using Core.Enums;
using Core.Models.Dtos;

namespace Business.Recording.Draft.Actions
{
    /// <summary>
    /// A check marked while recording is a search and nothing else: a wait until the element is
    /// there, or until it has gone, by its picture or its words, failing the execution when it times
    /// out. Every recorded check waits, so "it must be there" and "wait until it appears" are the one
    /// step. What comes after it goes in its Success branch, as after a click's search. With nothing
    /// to cut the picture from - the press kept no screenshot - it is no step.
    /// </summary>
    public static class CheckDraftBuilder
    {
        public static void Build(RecordedActionDto action, ReviewedActionDto review, RecordingDraft draft)
        {
            if (review.Choice == ReviewChoiceEnum.CHECK_GONE)
                draft.AddSearchFor(action, review, "Check", "Check icon", " gone", SearchModeEnum.WAIT_UNTIL_NOT_FOUND);
            else
                draft.AddSearchFor(action, review, "Check", "Check icon", string.Empty, SearchModeEnum.WAIT_UNTIL_FOUND);
        }
    }
}
