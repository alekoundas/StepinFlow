using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class PullRepositoryHandler
    {
        private readonly IBranchSwitchService _branchSwitchService;

        public PullRepositoryHandler(IBranchSwitchService branchSwitchService)
        {
            _branchSwitchService = branchSwitchService;
        }

        public async Task<ResultDto<RepositoryBranchSwitchDto>> HandleAsync(string remoteSha, CancellationToken ct)
        {
            try
            {
                return await _branchSwitchService.PullAsync(remoteSha, ct);
            }
            catch (Exception ex)
            {
                return ResultDto<RepositoryBranchSwitchDto>.Failure(ex.Message);
            }
        }
    }
}
