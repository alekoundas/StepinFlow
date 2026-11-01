using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers.Execution
{
    /// <summary>
    /// One run and every step of it, in the order they happened.
    /// </summary>
    public class GetExecutionHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetExecutionHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<ExecutionDto>> HandleAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            ExecutionDto? execution = await dbContext.Executions
                .AsNoTracking()
                .Where(x => x.Id == id)
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
                    CommitSha = x.CommitSha,
                    HasUncommittedChanges = x.HasUncommittedChanges,
                    Script = x.Script,
                    FlowId = x.FlowId,
                })
                .FirstOrDefaultAsync(ct);

            if (execution == null)
                return ResultDto<ExecutionDto>.Failure("That run no longer exists.");

            execution.ExecutionSteps = await dbContext.ExecutionSteps
                .AsNoTracking()
                .Where(x => x.ExecutionId == id)
                .OrderBy(x => x.Sequence)
                .Select(ExecutionStepProjection.Row)
                .ToListAsync(ct);

            ExecutionStepProjection.ClearUnwritableScores(execution.ExecutionSteps);

            return ResultDto<ExecutionDto>.Success(execution);
        }
    }
}
