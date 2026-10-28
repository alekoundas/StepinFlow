using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Draft.Actions
{
    /// <summary>
    /// A step that is not a click, marked while recording where one would have been: a command, an
    /// application opened, or an input's value typed. Where it was marked does not matter to any of
    /// them. One Review left with nothing to execute, open or type is no step.
    /// </summary>
    public static class SubstituteDraftBuilder
    {
        public static void Build(ReviewedActionDto review, RecordingDraft draft)
        {
            string command = review.CommandText?.Trim() ?? string.Empty;
            string inputName = review.InputName?.Trim() ?? string.Empty;

            switch (review.Choice)
            {
                case ReviewChoiceEnum.EXECUTE_COMMAND when command.Length > 0:
                    draft.Add(new FlowStep
                    {
                        FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND,
                        RunCommandPreset = RunCommandPresetEnum.CUSTOM,
                        RunCommandValue = command,
                    });
                    break;

                case ReviewChoiceEnum.LAUNCH_APP when command.Length > 0:
                    draft.Add(new FlowStep
                    {
                        FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND,
                        RunCommandPreset = RunCommandPresetEnum.LAUNCH_APP,
                        RunCommandValue = command,
                    });
                    break;

                case ReviewChoiceEnum.TYPE_INPUT when inputName.Length > 0:
                    FlowCsvColumn input = draft.InputNamed(inputName);
                    draft.Add(new FlowStep
                    {
                        FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT,
                        KeyboardInputType = KeyboardInputTypeEnum.TEXT,
                        KeyboardInputText = $"{{{{{input.Name}}}}}",
                    });
                    break;
            }
        }
    }
}
