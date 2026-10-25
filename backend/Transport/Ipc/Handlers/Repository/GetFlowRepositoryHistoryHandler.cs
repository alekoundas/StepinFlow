using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class GetFlowRepositoryHistoryHandler
    {
        private readonly IGitRepositoryService _gitRepositoryService;

        public GetFlowRepositoryHistoryHandler(IGitRepositoryService gitRepositoryService)
        {
            _gitRepositoryService = gitRepositoryService;
        }

        public async Task<ResultDto<List<RepositoryCommitDto>>> HandleAsync(int flowId, CancellationToken ct)
        {
            try
            {
                List<RepositoryCommitDto> commits = await _gitRepositoryService.GetFlowHistoryAsync(flowId, ct);
                return ResultDto<List<RepositoryCommitDto>>.Success(commits);
            }
            catch (Exception ex)
            {
                return ResultDto<List<RepositoryCommitDto>>.Failure(ex.Message);
            }
        }
    }
}
