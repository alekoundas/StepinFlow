using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class PreviewRepositoryPullHandler
    {
        private readonly IGitRepositoryService _gitRepositoryService;

        public PreviewRepositoryPullHandler(IGitRepositoryService gitRepositoryService)
        {
            _gitRepositoryService = gitRepositoryService;
        }

        public async Task<ResultDto<RepositoryPullDto>> HandleAsync(CancellationToken ct)
        {
            try
            {
                return await _gitRepositoryService.PreviewPullAsync(ct);
            }
            catch (Exception ex)
            {
                return ResultDto<RepositoryPullDto>.Failure(ex.Message);
            }
        }
    }
}
