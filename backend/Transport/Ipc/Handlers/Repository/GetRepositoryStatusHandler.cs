using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class GetRepositoryStatusHandler
    {
        private readonly IGitRepositoryService _gitRepositoryService;

        public GetRepositoryStatusHandler(IGitRepositoryService gitRepositoryService)
        {
            _gitRepositoryService = gitRepositoryService;
        }

        public async Task<ResultDto<RepositoryStatusDto>> HandleAsync(CancellationToken ct)
        {
            try
            {
                RepositoryStatusDto status = await _gitRepositoryService.GetStatusAsync(ct);
                return ResultDto<RepositoryStatusDto>.Success(status);
            }
            catch (Exception ex)
            {
                // Until startup has made the repository there is none to read, and the app asks again.
                return ResultDto<RepositoryStatusDto>.Failure(ex.Message);
            }
        }
    }
}
