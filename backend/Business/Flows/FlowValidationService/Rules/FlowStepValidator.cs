using Business.FlowScript.Catalogs;
using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Catalogs;
using Core.Models.Business.Ocr;

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
            FlowStepTypeEnum.WINDOW_CLOSE,
        ];

        // ================================================================
        // Public methods
        // ================================================================
        public static void Validate(
            IReadOnlyList<FlowStep> authoredSteps,
            IReadOnlyDictionary<int, int> templateCountByStepId,
            IReadOnlyList<FlowArea> areas,
            IReadOnlySet<string> installedReaders,
            FlowValidationResultDto result)
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
                        ValidateReader(result, step, installedReaders);

                        if (string.IsNullOrWhiteSpace(step.ConditionText))
                            result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.SEARCH_TEXT_MISSING, "There is no text to look for.");
                        break;

                    case FlowStepTypeEnum.SYSTEM_COMMAND:
                        ValidateCommand(result, step, areasById);
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

        // A reader is downloaded per machine, and a flow from a repository can name one this
        // machine does not have.
        private static void ValidateReader(FlowValidationResultDto result, FlowStep step, IReadOnlySet<string> installedReaders)
        {
            OcrReader? reader = OcrReaderCatalog.Find(step.OcrLanguage);
            if (reader == null)
            {
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.OCR_READER_NOT_DOWNLOADED, $"\"{step.OcrLanguage}\" is not a text reader this app knows. Pick one.");
                return;
            }

            if (installedReaders.Contains(reader.Id))
                return;

            if (reader.IsBuiltIn)
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.OCR_READER_NOT_DOWNLOADED, $"The {reader.Name} text reader is missing from the app. Reinstall it.");
            else
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.OCR_READER_NOT_DOWNLOADED, $"The {reader.Name} text reader is not downloaded on this machine. Download it in Settings, under Text recognition.");
        }

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
                step.ShellCommandValue,
                step.ShellWorkingDirectory,
                step.ShellArguments,
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
            else if (areasById.TryGetValue(step.FlowAreaId.Value, out FlowArea? area) && area.Type != FlowAreaTypeEnum.APPLICATION && area.Type != FlowAreaTypeEnum.BROWSER_TAB)
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.AREA_NOT_A_WINDOW, $"\"{area.Name}\" is not a window, so there is nothing to focus, resize, move or close. Pick an application or a browser tab.");

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RESIZE && (step.WindowWidth < 1 || step.WindowHeight < 1))
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.WINDOW_SIZE_MISSING, "Give the window a size to resize to.");

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RELOCATE && step.FlowPointId == null)
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.POINT_MISSING, "There is no point to move the window to.");

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_CLOSE && step.IsCloseForced && step.FlowAreaId != null
                && areasById.TryGetValue(step.FlowAreaId.Value, out FlowArea? tab) && tab.Type == FlowAreaTypeEnum.BROWSER_TAB)
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.CLOSE_FORCED_ON_TAB, $"\"{tab.Name}\" is a browser tab, and forcing it would end the whole browser, every tab in it. A tab closes the way Ctrl+W closes it, or stays open.");
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

        private static void ValidateCommand(FlowValidationResultDto result, FlowStep step, Dictionary<int, FlowArea> areasById)
        {
            if (step.ShellCommandMode == ShellCommandModeEnum.CUSTOM)
            {
                if (string.IsNullOrWhiteSpace(step.ShellCommandValue))
                    result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.COMMAND_MISSING, "There is no command to run.");
                return;
            }

            // The catalog decides which modes take a parameter, so this stays right when a
            // mode is added or changed.
            ShellCommandModeDto mode = ShellCommandModeCatalog.Get(step.ShellCommandMode);

            // A launch of an area starts what the area is started by, with its own arguments after.
            FlowArea? opens = null;
            if (step.ShellCommandMode == ShellCommandModeEnum.LAUNCH_APP && step.FlowAreaId != null)
                areasById.TryGetValue(step.FlowAreaId.Value, out opens);

            if (opens != null)
            {
                ValidateLaunchOf(result, step, opens);
                return;
            }

            if (mode.HasParameter && string.IsNullOrWhiteSpace(step.ShellCommandValue))
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.COMMAND_PARAMETER_MISSING, $"\"{mode.Label}\" needs a {mode.ParameterLabel.ToLowerInvariant()}.");
        }

        private static void ValidateLaunchOf(FlowValidationResultDto result, FlowStep step, FlowArea opens)
        {
            if (string.IsNullOrWhiteSpace(opens.LaunchCommand))
            {
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.COMMAND_PARAMETER_MISSING, $"Nothing starts \"{opens.Name}\". Say what does on the Areas tab, under Started by.");
                return;
            }

            // Steam takes a game's arguments in a link form of its own, so a space and the rest
            // would never reach the game.
            string program = CommandLineHelper.SplitProgram(opens.LaunchCommand, out _);
            if (!string.IsNullOrWhiteSpace(step.ShellArguments) && program.Contains("://", StringComparison.Ordinal))
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.LAUNCH_ARGUMENTS_ON_LINK, $"\"{opens.Name}\" is started by a link, and a link takes no arguments. A Steam game's are set in Steam, in its launch options.");
        }
    }
}
