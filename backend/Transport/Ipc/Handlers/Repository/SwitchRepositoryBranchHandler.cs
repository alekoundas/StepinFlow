using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class SwitchRepositoryBranchHandler
    {
        private readonly IBranchSwitchService _branchSwitchService;

        public SwitchRepositoryBranchHandler(IBranchSwitchService branchSwitchService)
        {
            _branchSwitchService = branchSwitchService;
        }

        public async Task<ResultDto<RepositoryBranchSwitchDto>> HandleAsync(string branch, CancellationToken ct)
        {
            try
            {
                return await _branchSwitchService.SwitchAsync(branch, ct);
            }
            catch (Exception ex)
            {
                return ResultDto<RepositoryBranchSwitchDto>.Failure(ex.Message);
            }
        }
    }
}
