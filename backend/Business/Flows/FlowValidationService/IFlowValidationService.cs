using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;

namespace Business.Flows.FlowValidationService
{
    public interface IFlowValidationService
    {
        /// <summary>
        /// Load the whole flow tree and do the actual validation by calling "Validate()".
        /// </summary>
        Task<FlowValidationResultDto> ValidateAsync(AppDbContext dbContext, int flowId, CancellationToken ct);

        /// <summary>
        /// Load the whole flow tree for all FlowIds and do the actual validation by calling "Validate()".
        /// </summary>
        Task<IReadOnlyDictionary<int, FlowValidationResultDto>> ValidateAsync(AppDbContext dbContext, IReadOnlyList<int> flowIds, CancellationToken ct);

        /// <summary>
        /// Per flow check references and form fields validity 
        /// </summary>
        FlowValidationResultDto Validate(
            IReadOnlyList<FlowStep> steps,
            IReadOnlyDictionary<int, int> templateCountByStepId,
            IReadOnlyList<FlowArea> areas,
            IReadOnlyList<FlowPoint> points,
            IReadOnlyList<string> flowNames);
    }
}
