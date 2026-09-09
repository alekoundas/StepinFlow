using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Services.FlowValidationService
{
    public interface IFlowValidationService
    {
        FlowValidationResultDto Validate(IReadOnlyList<FlowStep> steps, IReadOnlyDictionary<int, int> templateCountByStepId);
        IReadOnlyList<FlowCheck> GetChecks(IReadOnlyList<FlowCheckNode> steps);
    }
}
