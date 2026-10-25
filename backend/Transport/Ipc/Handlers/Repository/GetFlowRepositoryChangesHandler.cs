using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class GetFlowRepositoryChangesHandler
    {
        private readonly IGitRepositoryService _gitRepositoryService;

        public GetFlowRepositoryChangesHandler(IGitRepositoryService gitRepositoryService)
        {
            _gitRepositoryService = gitRepositoryService;
        }

        public async Task<ResultDto<RepositoryFlowChangesDto>> HandleAsync(int flowId, CancellationToken ct)
        {
            try
            {
                RepositoryFlowChangesDto? changes = await _gitRepositoryService.GetFlowChangesAsync(flowId, ct);
                if (changes == null)
                    return ResultDto<RepositoryFlowChangesDto>.Failure("That flow no longer exists.");

                return ResultDto<RepositoryFlowChangesDto>.Success(changes);
            }
            catch (Exception ex)
            {
                return ResultDto<RepositoryFlowChangesDto>.Failure(ex.Message);
            }
        }
    }
}
