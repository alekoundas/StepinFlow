using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Services.FlowValidationService.Rules
{
    /// <summary>
    /// Whether the steps still agree with each other - the search a cursor step reads, the branch
    /// it now sits in, whether a check changes anything. What a drag and drop quietly breaks.
    /// </summary>
    public sealed class FlowStructureValidator
    {
        private static readonly FlowStepTypeEnum[] CursorTypes =
        [
            FlowStepTypeEnum.CURSOR_CLICK,
            FlowStepTypeEnum.CURSOR_DRAG,
            FlowStepTypeEnum.CURSOR_SCROLL,
            FlowStepTypeEnum.CURSOR_RELOCATE,
        ];

        public FlowStructureValidator()
        {
        }


        // ================================================================
        // Public  methods
        // ================================================================

        public void Validate(
            IReadOnlyList<FlowStep> authoredSteps,
            IReadOnlyDictionary<int, StepChainNode> byStepId,
            ILookup<int?, FlowStep> childrenByParentId,
            IReadOnlyList<FlowCheck> checks,
            IReadOnlyList<string> areaAndPointNames,
            FlowValidationResultDto result)
        {
            foreach (FlowStep step in authoredSteps)
            {
                if (CursorTypes.Contains(step.FlowStepType))
                    ValidateCursor(result, step, byStepId);

                if (step.FlowStepType == FlowStepTypeEnum.CHECK_VALUE)
                    ValidateValueSource(result, step, byStepId);

                if (step.FlowStepType == FlowStepTypeEnum.NOTIFY)
                    ValidateNotify(result, step, byStepId);

                // A step that branches and has nothing in.
                if (TreeStepHelper.HasBranchChildren(step.FlowStepType) && IsEveryBranchEmpty(step, childrenByParentId))
                    result.Add(step, ValidationSeverityEnum.WARNING, FlowValidationCodeEnum.BRANCHES_EMPTY, "Success and Failure are both empty.");
            }

            ValidateChecksDecideSomething(authoredSteps, checks, result);
            ValidateNamesAreUnique(authoredSteps, areaAndPointNames, result);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Steps, areas and points are one namespace, because the script refers to all three by
        // name. A duplicate makes "Click at \"Find button\"" ambiguous, and makes execution history
        // correlate two different steps into one trend.
        private static void ValidateNamesAreUnique(IReadOnlyList<FlowStep> authoredSteps, IReadOnlyList<string> areaAndPointNames, FlowValidationResultDto result)
        {
            IReadOnlyList<string> duplicates = FlowNameHelper.Duplicates(
                authoredSteps.Select(x => x.Name).Concat(areaAndPointNames));

            if (duplicates.Count == 0)
                return;

            HashSet<string> duplicated = new HashSet<string>(duplicates, StringComparer.OrdinalIgnoreCase);

            foreach (FlowStep step in authoredSteps.Where(x => duplicated.Contains(x.Name.Trim())))
            {
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.NAME_DUPLICATE,
                    $"\"{step.Name}\" is used more than once. Steps, areas and points share one set of names, because the script refers to them by name.");
            }

            // A name shared by two areas, or an area and a point, has no step to hang the message
            // on - but it is the same problem and has to be sayable.
            foreach (string name in duplicates.Where(x => !authoredSteps.Any(s => string.Equals(s.Name.Trim(), x, StringComparison.OrdinalIgnoreCase))))
            {
                result.Add(null, name, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.NAME_DUPLICATE,
                    $"\"{name}\" is used by more than one area or point.");
            }
        }

        // A check whose failure stops nothing and whose result nothing reads is a check that was
        // never really made. The flow still passes with the application broken, which is the exact
        // failure the check model exists to prevent.
        private static void ValidateChecksDecideSomething(IReadOnlyList<FlowStep> authoredSteps, IReadOnlyList<FlowCheck> checks, FlowValidationResultDto result)
        {
            HashSet<int> referenced = authoredSteps
                .SelectMany(x => new[] { x.FlowStepReferenceId, x.FlowStepReferenceEndId })
                .Where(x => x != null)
                .Select(x => x!.Value)
                .ToHashSet();

            foreach (FlowCheck check in checks)
            {
                if (check.IsFatal || referenced.Contains(check.FlowStepId))
                    continue;

                result.Add(
                    check.FlowStepId,
                    check.Name,
                    ValidationSeverityEnum.WARNING,
                    FlowValidationCodeEnum.CHECK_DECIDES_NOTHING,
                    "Failing this changes nothing: no End Execution below it, and no step reads its result.");
            }
        }

        private static void ValidateCursor(FlowValidationResultDto result, FlowStep step, IReadOnlyDictionary<int, StepChainNode> byId)
        {
            ValidatePoint(result, step, byId, step.FlowPointId, step.FlowStepReferenceId, "");

            if (step.FlowStepType == FlowStepTypeEnum.CURSOR_DRAG)
                ValidatePoint(result, step, byId, step.FlowPointEndId, step.FlowStepReferenceEndId, "drop ");
        }

        private static void ValidatePoint(
            FlowValidationResultDto result,
            FlowStep step,
            IReadOnlyDictionary<int, StepChainNode> byId,
            int? flowPointId,
            int? referenceId,
            string label)
        {
            if (flowPointId != null)
                return;

            // Neither set says nothing about which was meant, so the message offers both.
            if (referenceId == null)
            {
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.POINT_MISSING, $"There is no {label}point to act on. Pick a saved point, or a search whose result gives one.");
                return;
            }

            if (!TreeStepHelper.CanReadResultOf(byId, step.Id, referenceId.Value))
            {
                string name;
                if (byId.TryGetValue(referenceId.Value, out StepChainNode reference))
                    name = reference.Name;
                else
                    name = "that step";

                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.STEP_RESULT_UNREACHABLE, $"The {label}point reads \"{name}\", which no longer runs above this step on the Success side.");
            }
        }

        private static void ValidateValueSource(FlowValidationResultDto result, FlowStep step, IReadOnlyDictionary<int, StepChainNode> byId)
        {
            if (step.FlowStepReferenceId == null)
            {
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.STEP_RESULT_MISSING, "Pick the step whose result is checked.");
                return;
            }

            if (!TreeStepHelper.CanReadResultOf(byId, step.Id, step.FlowStepReferenceId.Value))
            {
                string name;
                if (byId.TryGetValue(step.FlowStepReferenceId.Value, out StepChainNode reference))
                    name = reference.Name;
                else
                    name = "that step";

                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.STEP_RESULT_UNREACHABLE, $"This checks \"{name}\", which no longer runs above this step on the Success side.");
            }
        }

        private static void ValidateNotify(FlowValidationResultDto result, FlowStep step, IReadOnlyDictionary<int, StepChainNode> byId)
        {
            if (step.FlowStepReferenceId is int referenceId && !TreeStepHelper.CanReportFailureOf(byId, step.Id, referenceId))
                result.Add(step, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.FAILED_STEP_UNREACHABLE, "The step this reports on does not fail above it any more.");
        }

        private static bool IsEveryBranchEmpty(FlowStep step, ILookup<int?, FlowStep> childrenByParent)
        {
            return childrenByParent[step.Id].All(branch => !childrenByParent[branch.Id].Any());
        }
    }
}
