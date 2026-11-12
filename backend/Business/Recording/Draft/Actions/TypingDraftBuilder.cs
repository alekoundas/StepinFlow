using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Draft.Actions
{
    /// <summary>
    /// Typed text is an input with what was typed as its default, and the step types the input, so
    /// the value is changed in one place. A secret keeps no value here: it is set once on the
    /// Inputs tab and kept only in the database. Typed as written, the text is on the line itself -
    /// and so is a value typed wrong on purpose, which changing an input must never put right, with
    /// a comment saying so for the model.
    /// </summary>
    public static class TypingDraftBuilder
    {
        public const string WRONG_ON_PURPOSE_COMMENT = "wrong on purpose: the application should reject it";

        public static void Build(RecordedActionDto action, ReviewedActionDto review, RecordingDraft draft)
        {
            string typed = action.Text ?? string.Empty;
            string text = typed;

            if (review.Choice == ReviewChoiceEnum.INPUT)
            {
                string desired = string.IsNullOrWhiteSpace(review.InputName) ? "input" : review.InputName.Trim();
                FlowInput input = draft.Input(desired, typed, review.IsSecret);
                text = $"{{{{{input.Name}}}}}";
            }

            string comment = string.Empty;
            if (review.Choice == ReviewChoiceEnum.WRONG_ON_PURPOSE)
                comment = WRONG_ON_PURPOSE_COMMENT;

            draft.Add(new FlowStep
            {
                FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT,
                KeyboardInputType = KeyboardInputTypeEnum.TEXT,
                KeyboardInputText = text,
                CodeComment = comment,
            });
        }
    }
}
