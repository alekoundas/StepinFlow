using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class CreateRepositoryBranchHandler
    {
        private readonly IGitRepositoryService _gitRepositoryService;

        public CreateRepositoryBranchHandler(IGitRepositoryService gitRepositoryService)
        {
            _gitRepositoryService = gitRepositoryService;
        }

        public async Task<ResultDto<RepositoryBranchDto>> HandleAsync(string name, CancellationToken ct)
        {
            try
            {
                return await _gitRepositoryService.CreateBranchAsync(name, ct);
            }
            catch (Exception ex)
            {
                return ResultDto<RepositoryBranchDto>.Failure(ex.Message);
            }
        }
    }
}
