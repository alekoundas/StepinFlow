using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class GetFlowRepositoryCommitHandler
    {
        private readonly IGitRepositoryService _gitRepositoryService;

        public GetFlowRepositoryCommitHandler(IGitRepositoryService gitRepositoryService)
        {
            _gitRepositoryService = gitRepositoryService;
        }

        public async Task<ResultDto<RepositoryFlowCommitDto>> HandleAsync(RepositoryFlowCommitRequestDto dto, CancellationToken ct)
        {
            try
            {
                RepositoryFlowCommitDto? commit = await _gitRepositoryService.GetFlowCommitAsync(dto, ct);
                if (commit == null)
                    return ResultDto<RepositoryFlowCommitDto>.Failure("That commit or that flow is not there any more.");

                return ResultDto<RepositoryFlowCommitDto>.Success(commit);
            }
            catch (Exception ex)
            {
                return ResultDto<RepositoryFlowCommitDto>.Failure(ex.Message);
            }
        }
    }
}
