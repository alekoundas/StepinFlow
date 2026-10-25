using Business.Repository;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;

namespace Transport.Ipc.Handlers
{
    public class DiscardFlowChangesHandler
    {
        private readonly IFlowRestoreService _flowRestoreService;

        public DiscardFlowChangesHandler(IFlowRestoreService flowRestoreService)
        {
            _flowRestoreService = flowRestoreService;
        }

        public async Task<ResultDto<FlowScriptImportResultDto>> HandleAsync(int flowId, CancellationToken ct)
        {
            try
            {
                return await _flowRestoreService.DiscardAsync(flowId, ct);
            }
            catch (Exception ex)
            {
                return ResultDto<FlowScriptImportResultDto>.Failure(ex.Message);
            }
        }
    }
}
