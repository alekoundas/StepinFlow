using Business.Services.CommandService;
using Core.Enums;
using Core.Helpers;
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

        public FlowValidationResultDto Validate(
            IReadOnlyList<FlowStep> steps,
            IReadOnlyDictionary<int, int> templateCountByStepId)
        {
            FlowValidationResultDto result = new FlowValidationResultDto();

            List<FlowStep> authored = steps
                .Where(x => !TreeStepHelper.IsBranchChild(x.FlowStepType))
                .ToList();

            if (authored.Count == 0)
            {
                Add(result, null, string.Empty, ValidationSeverityEnum.ERROR,
                    FlowValidationCodeEnum.FLOW_HAS_NO_STEPS, "This flow has no steps yet.");
                return Finish(result);
            }

            Dictionary<int, FlowStep> byId = steps.ToDictionary(x => x.Id);
            ILookup<int?, FlowStep> childrenByParent = steps.ToLookup(x => x.ParentFlowStepId);

            foreach (FlowStep step in authored)
            {
                if (string.IsNullOrWhiteSpace(step.Name))
                    Add(result, step, ValidationSeverityEnum.WARNING,
                        FlowValidationCodeEnum.NAME_MISSING, "This step has no name.");

                if (CursorTypes.Contains(step.FlowStepType))
                    ValidateCursor(result, step, byId);

                if (WindowTypes.Contains(step.FlowStepType))
                    ValidateWindow(result, step);

                switch (step.FlowStepType)
                {
                    case FlowStepTypeEnum.IMAGE_SEARCH:
                        ValidateArea(result, step);

                        if (!templateCountByStepId.TryGetValue(step.Id, out int templates) || templates == 0)
                            Add(result, step, ValidationSeverityEnum.ERROR,
                                FlowValidationCodeEnum.NO_TEMPLATES, "There is nothing to look for: add a template.");
                        break;

                    case FlowStepTypeEnum.TEXT_SEARCH:
                        ValidateArea(result, step);

                        if (string.IsNullOrWhiteSpace(step.ConditionText))
                            Add(result, step, ValidationSeverityEnum.ERROR,
                                FlowValidationCodeEnum.SEARCH_TEXT_MISSING, "There is no text to look for.");

                        if (string.IsNullOrWhiteSpace(step.OcrLanguage))
                            Add(result, step, ValidationSeverityEnum.ERROR,
                                FlowValidationCodeEnum.OCR_LANGUAGE_MISSING, "Pick the language the text is written in.");
                        break;

                    case FlowStepTypeEnum.SYSTEM_COMMAND:
                        ValidateCommand(result, step);
                        break;

                    case FlowStepTypeEnum.LOOP:
                        if (!step.IsLoopInfinite && step.LoopCount < 1)
                            Add(result, step, ValidationSeverityEnum.ERROR,
                                FlowValidationCodeEnum.LOOP_COUNT_MISSING, "A loop runs at least once, or for ever.");
                        break;

                    case FlowStepTypeEnum.SUB_FLOW:
                        if (step.SubFlowId == null)
                            Add(result, step, ValidationSeverityEnum.ERROR,
                                FlowValidationCodeEnum.SUB_FLOW_MISSING, "Pick the flow to run.");
                        break;
                }

                // A step that branches and has nothing in either branch does its work and then
                // stops, which is almost never what was meant.
                if (TreeStepHelper.HasBranchChildren(step.FlowStepType) && IsEveryBranchEmpty(step, childrenByParent))
                    Add(result, step, ValidationSeverityEnum.WARNING,
                        FlowValidationCodeEnum.BRANCHES_EMPTY, "Success and Failure are both empty.");
            }

            return Finish(result);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static void ValidateCursor(FlowValidationResultDto result, FlowStep step, Dictionary<int, FlowStep> byId)
        {
            ValidatePoint(result, step, byId, step.IsPointCustom, step.FlowPointId, step.FlowStepReferenceId, "");

            if (step.FlowStepType == FlowStepTypeEnum.CURSOR_DRAG)
                ValidatePoint(result, step, byId, step.IsPointEndCustom, step.FlowPointEndId, step.FlowStepReferenceEndId, "drop ");
        }

        private static void ValidatePoint(
            FlowValidationResultDto result,
            FlowStep step,
            Dictionary<int, FlowStep> byId,
            bool isCustom,
            int? flowPointId,
            int? referenceId,
            string label)
        {
            if (isCustom)
            {
                if (flowPointId == null)
                    Add(result, step, ValidationSeverityEnum.ERROR,
                        FlowValidationCodeEnum.POINT_MISSING, $"There is no {label}point to act on.");
                return;
            }

            if (referenceId == null)
            {
                Add(result, step, ValidationSeverityEnum.ERROR,
                    FlowValidationCodeEnum.STEP_RESULT_MISSING, $"Pick the search whose result gives the {label}point.");
                return;
            }

            if (!CanReadResultOf(byId, step, referenceId.Value))
            {
                string name = byId.TryGetValue(referenceId.Value, out FlowStep? reference) ? reference.Name : "that step";

                Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.STEP_RESULT_UNREACHABLE,
                    $"The {label}point reads \"{name}\", which no longer runs above this step on the Success side.");
            }
        }

        /// <summary>
        /// A result only exists once the search that produced it has run and succeeded, so the
        /// referenced step has to be an ancestor and the way down to here has to be its Success
        /// branch. Anywhere else it is either the failure path or a step that may not have run.
        /// </summary>
        private static bool CanReadResultOf(Dictionary<int, FlowStep> byId, FlowStep step, int referenceId)
        {
            int childId = step.Id;
            int? currentId = step.ParentFlowStepId;

            // Bounded by the step count, so a corrupt parent chain cannot spin forever.
            int guard = byId.Count + 1;

            while (currentId != null && guard-- > 0)
            {
                if (!byId.TryGetValue(currentId.Value, out FlowStep? current))
                    return false;

                if (current.Id == referenceId)
                    return byId.TryGetValue(childId, out FlowStep? branch)
                        && branch.FlowStepType == FlowStepTypeEnum.SUCCESS;

                childId = current.Id;
                currentId = current.ParentFlowStepId;
            }

            return false;
        }

        private static void ValidateWindow(FlowValidationResultDto result, FlowStep step)
        {
            ValidateArea(result, step);

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RESIZE && (step.WindowWidth < 1 || step.WindowHeight < 1))
                Add(result, step, ValidationSeverityEnum.ERROR,
                    FlowValidationCodeEnum.WINDOW_SIZE_MISSING, "Give the window a size to resize to.");

            if (step.FlowStepType == FlowStepTypeEnum.WINDOW_RELOCATE && step.FlowPointId == null)
                Add(result, step, ValidationSeverityEnum.ERROR,
                    FlowValidationCodeEnum.POINT_MISSING, "There is no point to move the window to.");
        }

        private static void ValidateArea(FlowValidationResultDto result, FlowStep step)
        {
            if (step.FlowAreaId == null)
                Add(result, step, ValidationSeverityEnum.ERROR,
                    FlowValidationCodeEnum.AREA_MISSING, "There is no area to work in.");
        }

        private static void ValidateCommand(FlowValidationResultDto result, FlowStep step)
        {
            if (step.RunCommandPreset == RunCommandPresetEnum.CUSTOM)
            {
                if (string.IsNullOrWhiteSpace(step.RunCommand))
                    Add(result, step, ValidationSeverityEnum.ERROR,
                        FlowValidationCodeEnum.COMMAND_MISSING, "There is no command to run.");
                return;
            }

            // The catalog decides which presets take a parameter, so this stays right when a
            // preset is added or changed.
            CommandPresetDto preset = CommandPresetCatalog.Get(step.RunCommandPreset);

            if (preset.HasParameter && string.IsNullOrWhiteSpace(step.RunCommandPresetValue))
                Add(result, step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.COMMAND_PARAMETER_MISSING,
                    $"\"{preset.Label}\" needs a {preset.ParameterLabel.ToLowerInvariant()}.");
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
