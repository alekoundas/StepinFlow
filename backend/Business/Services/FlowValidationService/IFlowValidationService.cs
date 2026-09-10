using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Services.FlowValidationService
{
    public interface IFlowValidationService
    {
        /// <param name="areaAndPointNames">
        /// The flow's area and point names. They share one namespace with step names, so uniqueness
        /// cannot be judged from the steps alone.
        /// </param>
        FlowValidationResultDto Validate(
            IReadOnlyList<FlowStep> steps,
            IReadOnlyDictionary<int, int> templateCountByStepId,
            IReadOnlyList<string> areaAndPointNames);
        IReadOnlyList<FlowCheck> GetChecks(IReadOnlyList<FlowCheckNode> steps);
    }
}
