using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Draft.Actions
{
    /// <summary>
    /// A check marked while recording is a search and nothing else: a wait until the element is
    /// there, or until it has gone, failing the execution when it times out. Every recorded check
    /// waits, so "it must be there" and "wait until it appears" are the one step. What comes after
    /// it goes in its Success branch, as after a click's search.
    /// </summary>
    public static class CheckDraftBuilder
    {
        public static void Build(RecordedActionDto action, ReviewedActionDto review, RecordingDraft draft)
        {
            // Nothing to cut it from - the press kept no screenshot - and a check needs its picture.
            FlowStepTemplate? template = draft.Template(action, review);
            if (template == null)
                return;

            string text = draft.ElementOf(action)?.Text ?? string.Empty;
            string desired = RecordingDraft.NameFrom("Check", text, "Check icon");

            SearchModeEnum mode = SearchModeEnum.WAIT_UNTIL_FOUND;
            if (review.Choice == ReviewChoiceEnum.CHECK_GONE)
            {
                mode = SearchModeEnum.WAIT_UNTIL_NOT_FOUND;
                desired += " gone";
            }

            draft.AddSearch(action, draft.Name(desired), template, mode);
        }
    }
}
