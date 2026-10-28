using Business.Command;
using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Flows.FlowValidationService.Rules
{
    /// <summary>
    /// Whether each step is configured well enough to run, judged on its own fields.
    /// </summary>
    public static class FlowStepValidator
    {
        private static readonly FlowStepTypeEnum[] WindowTypes =
        [
            FlowStepTypeEnum.WINDOW_FOCUS,
            FlowStepTypeEnum.WINDOW_RESIZE,
            FlowStepTypeEnum.WINDOW_RELOCATE,
        ];

        // ================================================================
        // Public methods
        // ================================================================
        public static void Validate(IReadOnlyList<FlowStep> authoredSteps, IReadOnlyDictionary<int, int> templateCountByStepId, IReadOnlyList<FlowArea> areas, FlowValidationResultDto result)
        {
            Dictionary<int, FlowArea> areasById = areas.ToDictionary(x => x.Id);

            foreach (FlowStep step in authoredSteps)
            {
                if (string.IsNullOrWhiteSpace(step.Name))
                    result.Add(step, ValidationSeverityEnum.WARNING, FlowValidationCodeEnum.NAME_MISSING, "This step has no name.");

                ValidateQuotes(result, step);

                if (WindowTypes.Contains(step.FlowStepType))
                    ValidateWindow(result, step, areasById);

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

                        // A script does not carry it, so a text search from a file or a recording has none,
                        // and Windows reads in the languages it is set to, which differ between machines.
                        if (string.IsNullOrWhiteSpace(step.OcrLanguage))
                            result.Add(step, ValidationSeverityEnum.WARNING, FlowValidationCodeEnum.OCR_LANGUAGE_MISSING, "No language is picked, so it reads in the languages Windows is set to on each machine. Pick one so it reads the same everywhere.");
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

        // A FlowStep column cant contain <[ and ]> 
        private static void ValidateQuotes(FlowValidationResultDto result, FlowStep step)
        {
            string quoteOpen = ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.QUOTE_OPEN);
            string quoteClose = ScriptKeywordCatalog.GetTextOfKeyword(ScriptSymbolEnum.QUOTE_CLOSE);

            // A line each, so the end of one field and the start of the next never make a quote between them.
            string text = string.Join("\n",
                step.Name,
                step.Message,
                step.KeyboardInputText,
                step.RunCommandValue,
                step.RunCommandWorkingDirectory,
                step.ResultExtractPattern,
                step.ConditionText,
                step.ConditionTextEnd);

            if (!text.Contains(quoteOpen, StringComparison.Ordinal) && !text.Contains(quoteClose, StringComparison.Ordinal))
                return;

            result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.QUOTE_INSIDE,
                $"Text in this step can't contain \"{quoteOpen}\" or \"{quoteClose}\".");
        }

        private static void ValidateWindow(FlowValidationResultDto result, FlowStep step, Dictionary<int, FlowArea> areasById)
        {
            if (step.FlowAreaId == null)
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.AREA_MISSING, "Pick the window this step acts on.");
            else if (areasById.TryGetValue(step.FlowAreaId.Value, out FlowArea? area) && area.Type != FlowAreaTypeEnum.APPLICATION)
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.AREA_NOT_A_WINDOW, $"\"{area.Name}\" is not a window, so there is nothing to focus, resize or move. Pick a window area.");

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

            // Empty and not empty compare against nothing, and only between takes a second value.
            bool needsValue = step.ConditionType != ConditionTypeEnum.IS_EMPTY && step.ConditionType != ConditionTypeEnum.IS_NOT_EMPTY;
            if (needsValue && string.IsNullOrWhiteSpace(step.ConditionText))
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.CONDITION_VALUE_MISSING, "There is nothing to check the result against.");

            if (step.ConditionType == ConditionTypeEnum.BETWEEN && string.IsNullOrWhiteSpace(step.ConditionTextEnd))
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
