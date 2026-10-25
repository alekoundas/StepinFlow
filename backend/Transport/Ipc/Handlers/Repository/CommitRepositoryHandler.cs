using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class CommitRepositoryHandler
    {
        private readonly IGitRepositoryService _gitRepositoryService;

        public CommitRepositoryHandler(IGitRepositoryService gitRepositoryService)
        {
            _gitRepositoryService = gitRepositoryService;
        }

        public async Task<ResultDto<RepositoryCommitDto>> HandleAsync(RepositoryCommitRequestDto dto, CancellationToken ct)
        {
            try
            {
                return await _gitRepositoryService.CommitAsync(dto, ct);
            }
            catch (Exception ex)
            {
                // Git's own refusal - nothing left to commit, a file it cannot read - is the user's to see.
                return ResultDto<RepositoryCommitDto>.Failure(ex.Message);
            }
        }
    }
}
