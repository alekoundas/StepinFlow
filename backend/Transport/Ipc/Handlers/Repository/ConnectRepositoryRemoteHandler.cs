using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class ConnectRepositoryRemoteHandler
    {
        private readonly IGitRepositoryService _gitRepositoryService;

        public ConnectRepositoryRemoteHandler(IGitRepositoryService gitRepositoryService)
        {
            _gitRepositoryService = gitRepositoryService;
        }

        public async Task<ResultDto<RepositoryConnectDto>> HandleAsync(RepositoryConnectRequestDto dto, CancellationToken ct)
        {
            try
            {
                return await _gitRepositoryService.ConnectAsync(dto, ct);
            }
            catch (Exception ex)
            {
                return ResultDto<RepositoryConnectDto>.Failure(ex.Message);
            }
        }
    }
}
