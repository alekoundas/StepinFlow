using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;

namespace Business.Repository
{
    /// <summary>
    /// A flow brought back to a commit: its script and images imported over it, the way a file is,
    /// so its row and its executions stay.
    /// </summary>
    public interface IFlowRestoreService
    {
        /// <summary>The flow as the last commit has it, which leaves nothing to commit.</summary>
        Task<ResultDto<FlowScriptImportResultDto>> DiscardAsync(int flowId, CancellationToken ct);

        /// <summary>The flow as an older commit has it, which lands as a change not committed yet.</summary>
        Task<ResultDto<FlowScriptImportResultDto>> RestoreAsync(int flowId, string commitSha, CancellationToken ct);
    }
}
