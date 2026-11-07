using Core.Helpers;
using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers.Execution
{
    /// <summary>
    /// The executions page's feed: every flow's executions, or one flow's, newest first, a page at a
    /// time. A page starts below the last id the one before ended on, so an execution that starts
    /// meanwhile cannot push a row into the next page a second time. A flow the branch hides is left
    /// out, as it is from the flow list beside it.
    /// </summary>
    public class GetExecutionFeedHandler
    {
        private const int PAGE_SIZE = 50;

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetExecutionFeedHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<List<ExecutionFeedItemDto>>> HandleAsync(ExecutionFeedRequestDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            IQueryable<Core.Models.Database.Execution> executions = dbContext.Executions
                .AsNoTracking()
                .Where(x => !x.Flow.IsHidden);

            if (dto.FlowId != null)
                executions = executions.Where(x => x.FlowId == dto.FlowId);

            if (dto.BeforeId != null)
                executions = executions.Where(x => x.Id < dto.BeforeId);

            if (dto.Statuses.Count > 0)
                executions = executions.Where(x => dto.Statuses.Contains(x.Status));

            List<ExecutionFeedItemDto> page = await executions
                .OrderByDescending(x => x.Id)
                .Take(PAGE_SIZE)
                .Select(x => new ExecutionFeedItemDto
                {
                    Id = x.Id,
                    FlowId = x.FlowId,
                    FlowName = x.Flow.Name,
                    CreatedOn = AsUtc(x.CreatedOn),
                    CompletedAt = x.CompletedAt == null ? null : AsUtc(x.CompletedAt.Value),
                    Status = x.Status,
                    HistoryLevel = x.HistoryLevel,
                    StepCount = x.StepCount,
                    ErrorMessage = x.ErrorMessage,
                    ErrorStepName = dbContext.FlowSteps
                        .Where(step => step.Id == x.ErrorFlowStepId)
                        .Select(step => step.Name)
                        .FirstOrDefault(),
                    ScreenshotFolderPath = x.ScreenshotFolderName,
                    CommitSha = x.CommitSha,
                    HasUncommittedChanges = x.HasUncommittedChanges,
                })
                .ToListAsync(ct);

            foreach (ExecutionFeedItemDto item in page.Where(x => x.ScreenshotFolderPath != null))
                item.ScreenshotFolderPath = Path.Combine(PathHelper.GetExecutionHistoryDataPath(), item.ScreenshotFolderPath!);

            return ResultDto<List<ExecutionFeedItemDto>>.Success(page);
        }

        // SQLite gives a time back with no zone, which goes out with none and is read as local time.
        private static DateTime AsUtc(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
