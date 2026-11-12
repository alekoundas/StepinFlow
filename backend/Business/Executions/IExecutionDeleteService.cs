using Core.Models.Dtos;

namespace Business.Executions
{
    /// <summary>
    /// Deletes executions: their steps, their row and their screenshots folder, then hands the
    /// space back to the disk. The one executing now is never deleted.
    /// </summary>
    public interface IExecutionDeleteService
    {
        Task<ResultDto<bool>> DeleteAsync(int executionId, CancellationToken ct);

        /// <summary>Every execution of one flow but one still going. How many went.</summary>
        Task<ResultDto<int>> DeleteForFlowAsync(int flowId, CancellationToken ct);
    }
}
