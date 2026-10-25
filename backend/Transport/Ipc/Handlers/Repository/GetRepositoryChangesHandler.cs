using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class GetRepositoryChangesHandler
    {
        private readonly IGitRepositoryService _gitRepositoryService;

        public GetRepositoryChangesHandler(IGitRepositoryService gitRepositoryService)
        {
            _gitRepositoryService = gitRepositoryService;
        }

        public async Task<ResultDto<List<RepositoryFileChangeDto>>> HandleAsync(CancellationToken ct)
        {
            try
            {
                List<RepositoryFileChangeDto> changes = await _gitRepositoryService.GetChangesAsync(ct);
                return ResultDto<List<RepositoryFileChangeDto>>.Success(changes);
            }
            catch (Exception ex)
            {
                return ResultDto<List<RepositoryFileChangeDto>>.Failure(ex.Message);
            }
        }
    }
}
