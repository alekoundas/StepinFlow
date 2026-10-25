using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class GetRepositoryBranchesHandler
    {
        private readonly IGitRepositoryService _gitRepositoryService;

        public GetRepositoryBranchesHandler(IGitRepositoryService gitRepositoryService)
        {
            _gitRepositoryService = gitRepositoryService;
        }

        public async Task<ResultDto<List<RepositoryBranchDto>>> HandleAsync(CancellationToken ct)
        {
            try
            {
                List<RepositoryBranchDto> branches = await _gitRepositoryService.GetBranchesAsync(ct);
                return ResultDto<List<RepositoryBranchDto>>.Success(branches);
            }
            catch (Exception ex)
            {
                return ResultDto<List<RepositoryBranchDto>>.Failure(ex.Message);
            }
        }
    }
}
