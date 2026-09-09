using Business.Services.CommandService;
using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Services.FlowValidationService.Rules
{
    /// <summary>
    /// Whether each step is configured well enough to run, judged on its own fields.
    /// </summary>
    public sealed class FlowStepValidator
    {
        private static readonly FlowStepTypeEnum[] WindowTypes =
        [
            FlowStepTypeEnum.WINDOW_FOCUS,
            FlowStepTypeEnum.WINDOW_RESIZE,
            FlowStepTypeEnum.WINDOW_RELOCATE,
        ];

        public FlowStepValidator()
        {
        }

        // ================================================================
        // Public methods
        // ================================================================
        public void Validate(IReadOnlyList<FlowStep> authoredSteps, IReadOnlyDictionary<int, int> templateCountByStepId, FlowValidationResultDto result)
        {
            foreach (FlowStep step in authoredSteps)
            {
                if (string.IsNullOrWhiteSpace(step.Name))
                    result.Add(step, ValidationSeverityEnum.WARNING, FlowValidationCodeEnum.NAME_MISSING, "This step has no name.");

                if (WindowTypes.Contains(step.FlowStepType))
                    ValidateWindow(result, step);

                switch (step.FlowStepType)
                {
                    case FlowStepTypeEnum.SEARCH_IMAGE:
                        ValidateArea(result, step);

                        if (!templateCountByStepId.TryGetValue(step.Id, out int templates) || templates == 0)
                            result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.NO_TEMPLATES, "There is nothing to look for: add a template.");
                        break;

                    case FlowStepTypeEnum.SEARCH_TEXT:
                        ValidateArea(result, step);

                        if (string.IsNullOrWhiteSpace(step.ConditionText))
                            result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.SEARCH_TEXT_MISSING, "There is no text to look for.");

                        if (string.IsNullOrWhiteSpace(step.OcrLanguage))
                            result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.OCR_LANGUAGE_MISSING, "Pick the language the text is written in.");
                        break;

                    case FlowStepTypeEnum.SYSTEM_COMMAND:
                        ValidateCommand(result, step);
                        break;

                    case FlowStepTypeEnum.WAIT:
                        // Zero is "not a range" rather than a bad range, so it is not an error.
                        if (step.WaitForMillisecondsMax > 0 && step.WaitForMillisecondsMax <= step.WaitForMilliseconds)
                            result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.WAIT_RANGE_INVALID, "The longest wait has to be longer than the shortest.");
                        break;

                    case FlowStepTypeEnum.LOOP:
                        if (!step.IsLoopInfinite && step.LoopCount < 1)
                            result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.LOOP_COUNT_MISSING, "A loop runs at least once, or for ever.");
                        break;

                    case FlowStepTypeEnum.SUB_FLOW:
                        if (step.SubFlowId == null)
                            result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.SUB_FLOW_MISSING, "Pick the flow to run.");
                        break;

                    case FlowStepTypeEnum.NOTIFY:
                        if (step.DiscordBotId == null)
                            result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.DISCORD_BOT_MISSING, "Pick the bot to send through.");
                        break;

                    case FlowStepTypeEnum.CHECK_VALUE:
                        ValidateCondition(result, step);
                        break;
                }
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static void ValidateWindow(FlowValidationResultDto result, FlowStep step)
        {
            if (string.IsNullOrWhiteSpace(step.ProcessName) && string.IsNullOrWhiteSpace(step.TitlePattern))
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.WINDOW_MATCH_MISSING, "Pick an application, or type a title to match.");

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RESIZE && (step.WindowWidth < 1 || step.WindowHeight < 1))
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.WINDOW_SIZE_MISSING, "Give the window a size to resize to.");

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RELOCATE && step.FlowPointId == null)
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.POINT_MISSING, "There is no point to move the window to.");
        }

        private static void ValidateArea(FlowValidationResultDto result, FlowStep step)
        {
            if (step.FlowAreaId == null)
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.AREA_MISSING, "There is no area to work in.");
        }

        private static void ValidateCondition(FlowValidationResultDto result, FlowStep step)
        {
            if (step.ConditionType == null)
            {
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.CONDITION_TYPE_MISSING, "Pick what to check for.");
                return;
            }

            if (ConditionHelper.NeedsValue(step.ConditionType.Value) && string.IsNullOrWhiteSpace(step.ConditionText))
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.CONDITION_VALUE_MISSING, "There is nothing to check the result against.");

            if (ConditionHelper.NeedsSecondValue(step.ConditionType.Value) && string.IsNullOrWhiteSpace(step.ConditionTextEnd))
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.CONDITION_RANGE_INCOMPLETE, "A range needs both ends.");
        }

        private static void ValidateCommand(FlowValidationResultDto result, FlowStep step)
        {
            if (step.RunCommandPreset == RunCommandPresetEnum.CUSTOM)
            {
                if (string.IsNullOrWhiteSpace(step.RunCommandValue))
                    result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.COMMAND_MISSING, "There is no command to run.");
                return;
            }

            // The catalog decides which presets take a parameter, so this stays right when a
            // preset is added or changed.
            CommandPresetDto preset = CommandPresetCatalog.Get(step.RunCommandPreset);

            if (preset.HasParameter && string.IsNullOrWhiteSpace(step.RunCommandValue))
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.COMMAND_PARAMETER_MISSING, $"\"{preset.Label}\" needs a {preset.ParameterLabel.ToLowerInvariant()}.");
        }
    }
}
