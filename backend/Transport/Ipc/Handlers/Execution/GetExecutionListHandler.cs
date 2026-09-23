using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers.Execution
{
    /// <summary>
    /// Past runs of one flow, newest first. Capped, because nothing prunes the table yet and the
    /// history panel only ever shows the recent ones - see TODO.md, keep-last-X-runs retention.
    /// </summary>
    public class GetExecutionListHandler
    {
        private const int _maxRuns = 50;

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetExecutionListHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<List<ExecutionDto>>> HandleAsync(int flowId, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<ExecutionDto> executions = await dbContext.Executions
                .AsNoTracking()
                .Where(x => x.FlowId == flowId)
                .OrderByDescending(x => x.Id)
                .Take(_maxRuns)
                .Select(x => new ExecutionDto
                {
                    Id = x.Id,
                    CreatedOn = x.CreatedOn,
                    CompletedAt = x.CompletedAt,
                    Status = x.Status,
                    HistoryLevel = x.HistoryLevel,
                    StepCount = x.StepCount,
                    ErrorFlowStepId = x.ErrorFlowStepId,
                    ErrorMessage = x.ErrorMessage,
                    ScreenshotFolderName = x.ScreenshotFolderName,
                    FlowStructureHash = x.FlowStructureHash,
                    FlowId = x.FlowId,
                })
                .ToListAsync(ct);

            return ResultDto<List<ExecutionDto>>.Success(executions);
        }
    }
}
