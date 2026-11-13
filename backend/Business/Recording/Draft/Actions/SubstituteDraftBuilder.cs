using Core.Catalogs;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Draft.Actions
{
    /// <summary>
    /// A step that is not a click, marked while recording where one would have been: a command, an
    /// application opened, an input's value typed, a notification or a wait. Where it was marked
    /// matters only to a notification that reads the words there first and sends them. One Review
    /// left with nothing to execute, open, type, send through or wait for is no step. A stage begun
    /// there is none either: its name begins the stage at the next search, as any action's does.
    /// </summary>
    public static class SubstituteDraftBuilder
    {
        public static void Build(RecordedActionDto action, ReviewedActionDto review, RecordingDraft draft)
        {
            string command = review.CommandText?.Trim() ?? string.Empty;
            string inputName = review.InputName?.Trim() ?? string.Empty;

            switch (review.Choice)
            {
                case ReviewChoiceEnum.EXECUTE_COMMAND when command.Length > 0:
                    draft.Add(new FlowStep
                    {
                        Name = draft.Name("Command"),
                        FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND,
                        ShellCommandMode = ShellCommandModeEnum.CUSTOM,
                        ShellCommandValue = command,
                    });
                    break;

                case ReviewChoiceEnum.LAUNCH_APP when command.Length > 0:
                    draft.Add(new FlowStep
                    {
                        Name = draft.Name(ShellCommandModeCatalog.Get(ShellCommandModeEnum.LAUNCH_APP).Label),
                        FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND,
                        ShellCommandMode = ShellCommandModeEnum.LAUNCH_APP,
                        ShellCommandValue = command,
                    });
                    break;

                case ReviewChoiceEnum.TYPE_INPUT when inputName.Length > 0:
                    FlowInput input = draft.InputNamed(inputName);
                    draft.Add(new FlowStep
                    {
                        FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT,
                        KeyboardInputType = KeyboardInputTypeEnum.TEXT,
                        KeyboardInputText = $"{{{{{input.Name}}}}}",
                    });
                    break;

                // In the read's Success branch, with what it read; with nowhere to read, on its own
                // and with no result, since it sits under nothing to report on.
                case ReviewChoiceEnum.NOTIFY when review.DiscordBotId != null:
                    FlowStep notify = new FlowStep
                    {
                        FlowStepType = FlowStepTypeEnum.NOTIFY,
                        Message = review.Message?.Trim() ?? string.Empty,
                        DiscordBotId = review.DiscordBotId,
                    };

                    FlowStep? read = null;
                    if (review.IsReadingText)
                        read = draft.AddRead(action, review);

                    if (read == null)
                    {
                        draft.Add(notify);
                    }
                    else
                    {
                        notify.FlowStepReference = read;
                        draft.AddToSuccess(read, notify);
                    }
                    break;

                case ReviewChoiceEnum.WAIT when review.WaitMilliseconds > 0:
                    draft.Add(new FlowStep
                    {
                        FlowStepType = FlowStepTypeEnum.WAIT,
                        WaitForMilliseconds = review.WaitMilliseconds,
                    });
                    break;
            }
        }
    }
}
