using Business.Services.FlowValidationService.Helpers;
using Business.Services.FlowValidationService.Rules;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Services.FlowValidationService
{
    /// <summary>
    /// 1) Per flow validate references and form fields validity 
    /// 2) Per flow checks during execution - used in the Ai and reporting.
    /// </summary>
    public sealed class FlowValidationService : IFlowValidationService
    {
        private readonly FlowStepValidator _stepValidator;
        private readonly FlowStructureValidator _structureValidator;

        public FlowValidationService(FlowStepValidator stepValidator, FlowStructureValidator structureValidator)
        {
            _stepValidator = stepValidator;
            _structureValidator = structureValidator;
        }

        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Per flow validate references and form fields validity 
        /// </summary>
        public FlowValidationResultDto Validate(IReadOnlyList<FlowStep> steps, IReadOnlyDictionary<int, int> templateCountByStepId)
        {
            FlowValidationResultDto result = new FlowValidationResultDto();

            List<FlowStep> authored = steps
                .Where(x => !TreeStepHelper.IsBranchChild(x.FlowStepType))
                .ToList();

            if (authored.Count == 0)
            {
                result.Add(null, string.Empty, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.FLOW_HAS_NO_STEPS, "This flow has no steps yet.");
                return Finish(result);
            }

            Dictionary<int, StepChainNode> byStepId = steps.ToDictionary(x => x.Id, x => new StepChainNode(x.Id, x.ParentFlowStepId, x.FlowStepType, x.Name));
            ILookup<int?, FlowStep> childrenByParentId = steps.ToLookup(x => x.ParentFlowStepId);
            IReadOnlyList<FlowCheck> checks = GetChecks(steps.Select(ToCheckNode).ToList());

            _stepValidator.Validate(authored, templateCountByStepId, result);
            _structureValidator.Validate(authored, byStepId, childrenByParentId, checks, result);

            return Finish(result);
        }

        /// <summary>
        /// Per flow checks during execution - used in the Ai and reporting.
        /// </summary>
        public IReadOnlyList<FlowCheck> GetChecks(IReadOnlyList<FlowCheckNode> steps)
        {
            return FlowCheckHelper.Build(steps);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static FlowCheckNode ToCheckNode(FlowStep step)
        {
            return new FlowCheckNode
            {
                Id = step.Id,
                ParentFlowStepId = step.ParentFlowStepId,
                FlowStepType = step.FlowStepType,
                OrderNumber = step.OrderNumber,
                Name = step.Name,
                CodeComment = step.CodeComment,
                Message = step.Message,
                EndExecutionAsSuccess = step.EndExecutionAsSuccess,
            };
        }

        private static FlowValidationResultDto Finish(FlowValidationResultDto result)
        {
            result.HasErrors = result.Issues.Any(x => x.Severity == ValidationSeverityEnum.ERROR);
            return result;
        }
    }
}
