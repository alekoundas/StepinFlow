using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;
using Core.Models.Dtos.Repository;

namespace Transport.Ipc.Handlers
{
    public class RestoreFlowVersionHandler
    {
        private readonly IFlowRestoreService _flowRestoreService;

        public RestoreFlowVersionHandler(IFlowRestoreService flowRestoreService)
        {
            _flowRestoreService = flowRestoreService;
        }

        public async Task<ResultDto<FlowScriptImportResultDto>> HandleAsync(RepositoryFlowCommitRequestDto dto, CancellationToken ct)
        {
            try
            {
                return await _flowRestoreService.RestoreAsync(dto.FlowId, dto.Sha, ct);
            }
            catch (Exception ex)
            {
                return ResultDto<FlowScriptImportResultDto>.Failure(ex.Message);
            }
        }
    }
}
