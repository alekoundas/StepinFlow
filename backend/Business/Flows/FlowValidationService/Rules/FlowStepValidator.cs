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
            IReadOnlyDictionary<int, string> webhooklessBots,
            FlowValidationResultDto result)
        {
            Dictionary<int, FlowArea> areasById = areas.ToDictionary(x => x.Id);

            foreach (FlowStep step in authoredSteps)
            {
                if (string.IsNullOrWhiteSpace(step.Name))
                    result.Issues.Add(new FlowValidationIssueDto
                    {
                        FlowStepId = step.Id,
                        FlowStepName = step.Name,
                        Severity = ValidationSeverityEnum.WARNING,
                        Code = FlowValidationCodeEnum.NAME_MISSING,
                        Message = "This step has no name.",
                    });

                ValidateQuotes(result, step);

                if (WindowTypes.Contains(step.FlowStepType))
                    ValidateWindow(result, step, areasById);

                switch (step.FlowStepType)
                {
                    case FlowStepTypeEnum.SEARCH_IMAGE:
                        ValidateArea(result, step);

                        if (!templateCountByStepId.TryGetValue(step.Id, out int templates) || templates == 0)
                            result.Issues.Add(new FlowValidationIssueDto
                            {
                                FlowStepId = step.Id,
                                FlowStepName = step.Name,
                                Severity = ValidationSeverityEnum.ERROR,
                                Code = FlowValidationCodeEnum.NO_TEMPLATES,
                                Message = "There is nothing to look for: add a template.",
                            });
                        break;

                    case FlowStepTypeEnum.SEARCH_TEXT:
                        ValidateArea(result, step);
                        ValidateReader(result, step, installedReaders);

                        if (NeedsValue(step.ConditionType) && string.IsNullOrWhiteSpace(step.ConditionText))
                            result.Issues.Add(new FlowValidationIssueDto
                            {
                                FlowStepId = step.Id,
                                FlowStepName = step.Name,
                                Severity = ValidationSeverityEnum.ERROR,
                                Code = FlowValidationCodeEnum.SEARCH_TEXT_MISSING,
                                Message = "There is no text to look for.",
                            });
                        break;

                    case FlowStepTypeEnum.SYSTEM_COMMAND:
                        ValidateCommand(result, step, areasById);
                        break;

                    case FlowStepTypeEnum.WAIT:
                        // Zero is "not a range" rather than a bad range, so it is not an error.
                        if (step.WaitForMillisecondsMax > 0 && step.WaitForMillisecondsMax <= step.WaitForMilliseconds)
                            result.Issues.Add(new FlowValidationIssueDto
                            {
                                FlowStepId = step.Id,
                                FlowStepName = step.Name,
                                Severity = ValidationSeverityEnum.ERROR,
                                Code = FlowValidationCodeEnum.WAIT_RANGE_INVALID,
                                Message = "The longest wait has to be longer than the shortest.",
                            });
                        break;

                    case FlowStepTypeEnum.LOOP:
                        if (!step.IsLoopInfinite && step.LoopCount < 1)
                            result.Issues.Add(new FlowValidationIssueDto
                            {
                                FlowStepId = step.Id,
                                FlowStepName = step.Name,
                                Severity = ValidationSeverityEnum.ERROR,
                                Code = FlowValidationCodeEnum.LOOP_COUNT_MISSING,
                                Message = "A loop runs at least once, or for ever.",
                            });
                        break;

                    case FlowStepTypeEnum.SUB_FLOW:
                        if (step.SubFlowId == null)
                            result.Issues.Add(new FlowValidationIssueDto
                            {
                                FlowStepId = step.Id,
                                FlowStepName = step.Name,
                                Severity = ValidationSeverityEnum.ERROR,
                                Code = FlowValidationCodeEnum.SUB_FLOW_MISSING,
                                Message = "Pick the flow to run.",
                            });
                        break;

                    case FlowStepTypeEnum.NOTIFY:
                        if (step.DiscordBotId == null)
                            result.Issues.Add(new FlowValidationIssueDto
                            {
                                FlowStepId = step.Id,
                                FlowStepName = step.Name,
                                Severity = ValidationSeverityEnum.ERROR,
                                Code = FlowValidationCodeEnum.DISCORD_BOT_MISSING,
                                Message = "Pick the bot to send through.",
                            });
                        else if (webhooklessBots.TryGetValue(step.DiscordBotId.Value, out string? bot))
                            result.Issues.Add(new FlowValidationIssueDto
                            {
                                FlowStepId = step.Id,
                                FlowStepName = step.Name,
                                Severity = ValidationSeverityEnum.WARNING,
                                Code = FlowValidationCodeEnum.DISCORD_BOT_NO_WEBHOOK,
                                Message = $"\"{bot}\" has no webhook yet, so this sends nothing. Paste it in under Settings, Discord bots.",
                            });
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
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.OCR_READER_NOT_DOWNLOADED,
                    Message = $"\"{step.OcrLanguage}\" is not a text reader this app knows. Pick one.",
                });
                return;
            }

            if (installedReaders.Contains(reader.Id))
                return;

            if (reader.IsBuiltIn)
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.OCR_READER_NOT_DOWNLOADED,
                    Message = $"The {reader.Name} text reader is missing from the app. Reinstall it.",
                });
            else
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.OCR_READER_NOT_DOWNLOADED,
                    Message = $"The {reader.Name} text reader is not downloaded on this machine. Download it in Settings, under Text recognition.",
                });
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

            result.Issues.Add(new FlowValidationIssueDto
            {
                FlowStepId = step.Id,
                FlowStepName = step.Name,
                Severity = ValidationSeverityEnum.ERROR,
                Code = FlowValidationCodeEnum.QUOTE_INSIDE,
                Message = $"Text in this step can't contain \"{quoteOpen}\" or \"{quoteClose}\".",
            });
        }

        private static void ValidateWindow(FlowValidationResultDto result, FlowStep step, Dictionary<int, FlowArea> areasById)
        {
            if (step.FlowAreaId == null)
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.AREA_MISSING,
                    Message = "Pick the window this step acts on.",
                });
            else if (areasById.TryGetValue(step.FlowAreaId.Value, out FlowArea? area) && area.Type != FlowAreaTypeEnum.APPLICATION && area.Type != FlowAreaTypeEnum.BROWSER_TAB)
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.AREA_NOT_A_WINDOW,
                    Message = $"\"{area.Name}\" is not a window, so there is nothing to focus, resize, move or close. Pick an application or a browser tab.",
                });

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RESIZE && (step.WindowWidth < 1 || step.WindowHeight < 1))
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.WINDOW_SIZE_MISSING,
                    Message = "Give the window a size to resize to.",
                });

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RELOCATE && step.FlowPointId == null)
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.POINT_MISSING,
                    Message = "There is no point to move the window to.",
                });

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_CLOSE && step.IsCloseForced && step.FlowAreaId != null
                && areasById.TryGetValue(step.FlowAreaId.Value, out FlowArea? tab) && tab.Type == FlowAreaTypeEnum.BROWSER_TAB)
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.CLOSE_FORCED_ON_TAB,
                    Message = $"\"{tab.Name}\" is a browser tab, and forcing it would end the whole browser, every tab in it. A tab closes the way Ctrl+W closes it, or stays open.",
                });
        }

        private static void ValidateArea(FlowValidationResultDto result, FlowStep step)
        {
            if (step.FlowAreaId == null)
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.AREA_MISSING,
                    Message = "There is no area to work in.",
                });
        }

        private static void ValidateCondition(FlowValidationResultDto result, FlowStep step)
        {
            if (step.ConditionType == null)
            {
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.CONDITION_TYPE_MISSING,
                    Message = "Pick what to check for.",
                });
                return;
            }

            // Only between takes a second value.
            if (NeedsValue(step.ConditionType) && string.IsNullOrWhiteSpace(step.ConditionText))
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.CONDITION_VALUE_MISSING,
                    Message = "There is nothing to check the result against.",
                });

            if (step.ConditionType == ConditionTypeEnum.BETWEEN && string.IsNullOrWhiteSpace(step.ConditionTextEnd))
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.CONDITION_RANGE_INCOMPLETE,
                    Message = "A range needs both ends.",
                });
        }

        // Empty and not empty compare against nothing.
        private static bool NeedsValue(ConditionTypeEnum? condition)
        {
            return condition != ConditionTypeEnum.IS_EMPTY && condition != ConditionTypeEnum.IS_NOT_EMPTY;
        }

        private static void ValidateCommand(FlowValidationResultDto result, FlowStep step, Dictionary<int, FlowArea> areasById)
        {
            if (step.ShellCommandMode == ShellCommandModeEnum.CUSTOM)
            {
                if (string.IsNullOrWhiteSpace(step.ShellCommandValue))
                    result.Issues.Add(new FlowValidationIssueDto
                    {
                        FlowStepId = step.Id,
                        FlowStepName = step.Name,
                        Severity = ValidationSeverityEnum.ERROR,
                        Code = FlowValidationCodeEnum.COMMAND_MISSING,
                        Message = "There is no command to run.",
                    });
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
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.COMMAND_PARAMETER_MISSING,
                    Message = $"\"{mode.Label}\" needs a {mode.ParameterLabel.ToLowerInvariant()}.",
                });
        }

        private static void ValidateLaunchOf(FlowValidationResultDto result, FlowStep step, FlowArea opens)
        {
            if (string.IsNullOrWhiteSpace(opens.LaunchCommand))
            {
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.COMMAND_PARAMETER_MISSING,
                    Message = $"Nothing starts \"{opens.Name}\". Say what does on the Areas tab, under Started by.",
                });
                return;
            }

            // Steam takes a game's arguments in a link form of its own, so a space and the rest
            // would never reach the game.
            string program = CommandLineHelper.SplitProgram(opens.LaunchCommand, out _);
            if (!string.IsNullOrWhiteSpace(step.ShellArguments) && program.Contains("://", StringComparison.Ordinal))
                result.Issues.Add(new FlowValidationIssueDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    Severity = ValidationSeverityEnum.ERROR,
                    Code = FlowValidationCodeEnum.LAUNCH_ARGUMENTS_ON_LINK,
                    Message = $"\"{opens.Name}\" is started by a link, and a link takes no arguments. A Steam game's are set in Steam, in its launch options.",
                });
        }
    }
}
