using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Services.FlowValidationService
{
    public interface IFlowValidator
    {
        /// <summary>
        /// Structural checks only: everything answerable from the steps themselves. Whether a
        /// window is open or a language is installed changes minute to minute and belongs to the
        /// run, not to the tree.
        /// </summary>
        FlowValidationResultDto Validate(
            IReadOnlyList<FlowStep> steps,
            IReadOnlyDictionary<int, int> templateCountByStepId);
    }
}
