using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Flows.FlowValidationService.Rules
{
    /// <summary>
    /// What only a QA test is held to: a check that can fail it. One whose failure ends nothing
    /// changes nothing when it fails, and a suite of those is green whatever the application does.
    /// A note rather than a warning, because a check that only decides which way the flow goes is
    /// fine, and the draft already ends every Failure branch with End Execution failed.
    /// </summary>
    public static class FlowQaTestValidator
    {
        public static void Validate(IReadOnlyList<FlowStep> steps, FlowValidationResultDto result)
        {
            Dictionary<int, FlowStep> byId = steps.ToDictionary(x => x.Id);

            List<FlowCheckNode> nodes = steps
                .Select(x => new FlowCheckNode
                {
                    Id = x.Id,
                    ParentFlowStepId = x.ParentFlowStepId,
                    FlowStepType = x.FlowStepType,
                    OrderNumber = x.OrderNumber,
                    Name = x.Name,
                    EndExecutionAsSuccess = x.EndExecutionAsSuccess,
                })
                .ToList();

            foreach (FlowCheck check in FlowCheckListHelper.Build(nodes).Where(x => !x.IsFatal))
            {
                // Under an End Execution the verdict is already decided, so nothing there could fail it.
                if (IsUnderEndExecution(byId[check.FlowStepId], byId))
                    continue;

                result.Add(check.FlowStepId, check.Name, ValidationSeverityEnum.INFO, FlowValidationCodeEnum.CHECK_CANNOT_FAIL,
                    "Failing this check never fails the execution: nothing under its Failure branch ends it. Add End Execution failed there if the test is meant to prove it.");
            }
        }

        private static bool IsUnderEndExecution(FlowStep step, IReadOnlyDictionary<int, FlowStep> byId)
        {
            int? currentId = step.ParentFlowStepId;
            int guard = byId.Count + 1;

            while (currentId != null && guard-- > 0)
            {
                if (!byId.TryGetValue(currentId.Value, out FlowStep? current))
                    return false;

                if (current.FlowStepType == FlowStepTypeEnum.END_EXECUTION)
                    return true;

                currentId = current.ParentFlowStepId;
            }

            return false;
        }
    }
}
