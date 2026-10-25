using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class PushRepositoryHandler
    {
        private readonly IGitRepositoryService _gitRepositoryService;

        public PushRepositoryHandler(IGitRepositoryService gitRepositoryService)
        {
            _gitRepositoryService = gitRepositoryService;
        }

        public async Task<ResultDto<RepositoryPushDto>> HandleAsync(CancellationToken ct)
        {
            try
            {
                return await _gitRepositoryService.PushAsync(ct);
            }
            catch (Exception ex)
            {
                return ResultDto<RepositoryPushDto>.Failure(ex.Message);
            }
        }
    }
}
