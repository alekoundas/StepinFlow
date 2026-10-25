using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Business.Repository
{
    public interface IBranchSwitchService
    {
        /// <summary>
        /// Checks the branch out and brings every flow to its version: the ones it changed or added
        /// imported, the ones it does not have hidden. Refused while anything is not committed or a
        /// flow is executing. REPOSITORY_BRANCH_SWITCH says how far it is.
        /// </summary>
        Task<ResultDto<RepositoryBranchSwitchDto>> SwitchAsync(string branch, CancellationToken ct);
    }
}
