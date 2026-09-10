using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Services.FlowValidationService
{
    public interface IFlowValidationService
    {
        /// <param name="flowNames">
        /// Every name the flow defines that is not a step - areas, points and csv columns. They
        /// share one namespace with step names, so neither uniqueness nor whether a {{name}} can
        /// resolve is answerable from the steps alone.
        /// </param>
        FlowValidationResultDto Validate(
            IReadOnlyList<FlowStep> steps,
            IReadOnlyDictionary<int, int> templateCountByStepId,
            IReadOnlyList<string> flowNames);
        IReadOnlyList<FlowCheck> GetChecks(IReadOnlyList<FlowCheckNode> steps);
    }
}
