using Business.Services.CommandService;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Services.FlowValidationService
{
    /// <summary>
    /// Answers "is this flow broken", once per flow.
    ///
    /// Deliberately not a second copy of the form rules: zod already says a name is required while
    /// you type. What only this can see is whether one step still agrees with another - the search
    /// a cursor step reads, the branch it now sits in - which is exactly what a drag and drop can
    /// quietly break.
    ///
    /// Deleting an area or a point nulls the foreign key that pointed at it, so "gone" and "never
    /// set" arrive here as the same thing and need only one rule between them.
    /// </summary>
    public sealed class FlowValidator : IFlowValidator
    {
        private static readonly FlowStepTypeEnum[] CursorTypes =
        [
            FlowStepTypeEnum.CURSOR_CLICK,
            FlowStepTypeEnum.CURSOR_DRAG,
            FlowStepTypeEnum.CURSOR_SCROLL,
            FlowStepTypeEnum.CURSOR_RELOCATE,
        ];

        private static readonly FlowStepTypeEnum[] WindowTypes =
        [
            FlowStepTypeEnum.WINDOW_FOCUS,
            FlowStepTypeEnum.WINDOW_RESIZE,
            FlowStepTypeEnum.WINDOW_RELOCATE,
        ];

        public FlowValidationResultDto Validate(IReadOnlyList<FlowStep> steps, IReadOnlyDictionary<int, int> templateCountByStepId)
        {
            FlowValidationResultDto result = new FlowValidationResultDto();

            List<FlowStep> authored = steps
                .Where(x => !TreeStepHelper.IsBranchChild(x.FlowStepType))
                .ToList();

            if (authored.Count == 0)
            {
                Add(result, null, string.Empty, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.FLOW_HAS_NO_STEPS, "This flow has no steps yet.");
                return Finish(result);
            }

            Dictionary<int, StepChainNode> byId = steps.ToDictionary(x => x.Id, x => new StepChainNode(x.Id, x.ParentFlowStepId, x.FlowStepType, x.Name));
            ILookup<int?, FlowStep> childrenByParent = steps.ToLookup(x => x.ParentFlowStepId);

            foreach (FlowStep step in authored)
            {
                if (string.IsNullOrWhiteSpace(step.Name))
                    Add(result, step, ValidationSeverityEnum.WARNING, FlowValidationCodeEnum.NAME_MISSING, "This step has no name.");

                if (CursorTypes.Contains(step.FlowStepType))
                    ValidateCursor(result, step, byId);

                if (WindowTypes.Contains(step.FlowStepType))
                    ValidateWindow(result, step);

                switch (step.FlowStepType)
                {
                    case FlowStepTypeEnum.IMAGE_SEARCH:
                        ValidateArea(result, step);

                        if (!templateCountByStepId.TryGetValue(step.Id, out int templates) || templates == 0)
                            Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.NO_TEMPLATES, "There is nothing to look for: add a template.");
                        break;

                    case FlowStepTypeEnum.READ_TEXT:
                        ValidateArea(result, step);

                        // Reading once succeeds on having read anything, so only the waiting modes
                        // need something to wait for.
                        if (step.SearchMode is SearchModeEnum.WAIT_UNTIL_FOUND or SearchModeEnum.WAIT_UNTIL_NOT_FOUND
                            && string.IsNullOrWhiteSpace(step.ConditionText))
                            Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.SEARCH_TEXT_MISSING, "There is no text to wait for.");

                        if (string.IsNullOrWhiteSpace(step.OcrLanguage))
                            Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.OCR_LANGUAGE_MISSING, "Pick the language the text is written in.");
                        break;

                    case FlowStepTypeEnum.SYSTEM_COMMAND:
                        ValidateCommand(result, step);
                        break;

                    case FlowStepTypeEnum.CHECK_VALUE:
                        ValidateCheckValue(result, step, byId);
                        break;

                    case FlowStepTypeEnum.LOOP:
                        if (!step.IsLoopInfinite && step.LoopCount < 1)
                            Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.LOOP_COUNT_MISSING, "A loop runs at least once, or for ever.");
                        break;

                    case FlowStepTypeEnum.SUB_FLOW:
                        if (step.InvokedFlowId == null)
                            Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.SUB_FLOW_MISSING, "Pick the flow to run.");
                        break;
                }

                // A step that branches and has nothing in either branch does its work and then
                // stops, which is almost never what was meant.
                if (TreeStepHelper.HasBranchChildren(step.FlowStepType) && IsEveryBranchEmpty(step, childrenByParent))
                    Add(result, step, ValidationSeverityEnum.WARNING, FlowValidationCodeEnum.BRANCHES_EMPTY, "Success and Failure are both empty.");
            }

            return Finish(result);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static void ValidateCursor(FlowValidationResultDto result, FlowStep step, IReadOnlyDictionary<int, StepChainNode> byId)
        {
            ValidatePoint(result, step, byId, step.IsPointCustom, step.FlowPointId, step.FlowStepReferenceId, "");

            if (step.FlowStepType == FlowStepTypeEnum.CURSOR_DRAG)
                ValidatePoint(result, step, byId, step.IsPointEndCustom, step.FlowPointEndId, step.FlowStepReferenceEndId, "drop ");
        }

        private static void ValidatePoint(
            FlowValidationResultDto result,
            FlowStep step,
            IReadOnlyDictionary<int, StepChainNode> byId,
            bool isCustom,
            int? flowPointId,
            int? referenceId,
            string label)
        {
            if (isCustom)
            {
                if (flowPointId == null)
                    Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.POINT_MISSING, $"There is no {label}point to act on.");
                return;
            }

            if (referenceId == null)
            {
                Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.STEP_RESULT_MISSING, $"Pick the search whose result gives the {label}point.");
                return;
            }

            if (!TreeStepHelper.CanReadResultOf(byId, step.Id, referenceId.Value))
            {
                string name = byId.TryGetValue(referenceId.Value, out StepChainNode reference) ? reference.Name : "that step";
                Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.STEP_RESULT_UNREACHABLE, $"The {label}point reads \"{name}\", which no longer runs above this step on the Success side.");
            }
        }

        private static void ValidateCheckValue(FlowValidationResultDto result, FlowStep step, IReadOnlyDictionary<int, StepChainNode> byId)
        {
            if (step.FlowStepReferenceId == null)
            {
                Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.STEP_RESULT_MISSING, "Pick the step whose result is checked.");
            }
            else if (!TreeStepHelper.CanReadResultOf(byId, step.Id, step.FlowStepReferenceId.Value))
            {
                string name = byId.TryGetValue(step.FlowStepReferenceId.Value, out StepChainNode reference) ? reference.Name : "that step";
                Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.STEP_RESULT_UNREACHABLE, $"This checks \"{name}\", which no longer runs above this step on the Success side.");
            }

            if (step.ConditionType == null)
            {
                Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.CONDITION_TYPE_MISSING, "Pick what to check for.");
                return;
            }

            if (ConditionHelper.NeedsValue(step.ConditionType.Value) && string.IsNullOrWhiteSpace(step.ConditionText))
                Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.CONDITION_VALUE_MISSING, "There is nothing to check the result against.");

            if (ConditionHelper.NeedsSecondValue(step.ConditionType.Value) && string.IsNullOrWhiteSpace(step.ConditionTextEnd))
                Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.CONDITION_RANGE_INCOMPLETE, "A range needs both ends.");
        }

        private static void ValidateWindow(FlowValidationResultDto result, FlowStep step)
        {
            ValidateArea(result, step);

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RESIZE && (step.WindowWidth < 1 || step.WindowHeight < 1))
                Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.WINDOW_SIZE_MISSING, "Give the window a size to resize to.");

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RELOCATE && step.FlowPointId == null)
                Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.POINT_MISSING, "There is no point to move the window to.");
        }

        private static void ValidateArea(FlowValidationResultDto result, FlowStep step)
        {
            if (step.FlowAreaId == null)
                Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.AREA_MISSING, "There is no area to work in.");
        }

        private static void ValidateCommand(FlowValidationResultDto result, FlowStep step)
        {
            if (step.RunCommandPreset == RunCommandPresetEnum.CUSTOM)
            {
                if (string.IsNullOrWhiteSpace(step.RunCommand))
                    Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.COMMAND_MISSING, "There is no command to run.");
                return;
            }

            // The catalog decides which presets take a parameter, so this stays right when a
            // preset is added or changed.
            CommandPresetDto preset = CommandPresetCatalog.Get(step.RunCommandPreset);

            if (preset.HasParameter && string.IsNullOrWhiteSpace(step.RunCommandPresetValue))
                Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.COMMAND_PARAMETER_MISSING, $"\"{preset.Label}\" needs a {preset.ParameterLabel.ToLowerInvariant()}.");
        }

        private static bool IsEveryBranchEmpty(FlowStep step, ILookup<int?, FlowStep> childrenByParent) =>
            childrenByParent[step.Id].All(branch => !childrenByParent[branch.Id].Any());

        private static void Add(
            FlowValidationResultDto result,
            FlowStep step,
            ValidationSeverityEnum severity,
            FlowValidationCodeEnum code,
            string message) =>
            Add(result, step.Id, step.Name, severity, code, message);

        private static void Add(
            FlowValidationResultDto result,
            int? stepId,
            string stepName,
            ValidationSeverityEnum severity,
            FlowValidationCodeEnum code,
            string message) =>
            result.Issues.Add(new FlowValidationIssueDto
            {
                FlowStepId = stepId,
                FlowStepName = stepName,
                Severity = severity,
                Code = code,
                Message = message,
            });

        private static FlowValidationResultDto Finish(FlowValidationResultDto result)
        {
            result.HasErrors = result.Issues.Any(x => x.Severity == ValidationSeverityEnum.ERROR);
            return result;
        }
    }
}
