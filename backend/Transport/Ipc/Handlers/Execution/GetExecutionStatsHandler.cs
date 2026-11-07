using Core.Enums;
using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers.Execution
{
    /// <summary>
    /// How executions ended, counted by outcome: every flow's or one flow's, over every day or the
    /// last few. A flow the branch hides is left out, as it is from the feed.
    /// </summary>
    public class GetExecutionStatsHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly TimeProvider _timeProvider;

        public GetExecutionStatsHandler(IDbContextFactory<AppDbContext> dbContextFactory, TimeProvider timeProvider)
        {
            _dbContextFactory = dbContextFactory;
            _timeProvider = timeProvider;
        }

        public async Task<ResultDto<ExecutionStatsDto>> HandleAsync(ExecutionStatsRequestDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            IQueryable<Core.Models.Database.Execution> executions = dbContext.Executions
                .AsNoTracking()
                .Where(x => !x.Flow.IsHidden);

            if (dto.FlowId != null)
                executions = executions.Where(x => x.FlowId == dto.FlowId);

            if (dto.Days != null)
            {
                DateTime since = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-dto.Days.Value);
                executions = executions.Where(x => x.CreatedOn >= since);
            }

            Dictionary<ExecutionStatusEnum, int> counts = await executions
                .GroupBy(x => x.Status)
                .Select(x => new { Status = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

            DateTime? lastFailureOn = await executions
                .Where(x => x.Status == ExecutionStatusEnum.FAILED || x.Status == ExecutionStatusEnum.ERRORED)
                .OrderByDescending(x => x.Id)
                .Select(x => (DateTime?)x.CreatedOn)
                .FirstOrDefaultAsync(ct);

            List<Took> completed = await executions
                .Where(x => x.Status == ExecutionStatusEnum.COMPLETED && x.CompletedAt != null)
                .Select(x => new Took(x.CreatedOn, x.CompletedAt!.Value))
                .ToListAsync(ct);

            ExecutionStatsDto stats = new ExecutionStatsDto
            {
                Total = counts.Values.Sum(),
                Executing = counts.GetValueOrDefault(ExecutionStatusEnum.RUNNING),
                Completed = counts.GetValueOrDefault(ExecutionStatusEnum.COMPLETED),
                Failed = counts.GetValueOrDefault(ExecutionStatusEnum.FAILED),
                Errored = counts.GetValueOrDefault(ExecutionStatusEnum.ERRORED),
                Inconclusive = counts.GetValueOrDefault(ExecutionStatusEnum.INCONCLUSIVE),
                Stopped = counts.GetValueOrDefault(ExecutionStatusEnum.STOPPED),
                Abandoned = counts.GetValueOrDefault(ExecutionStatusEnum.ABANDONED),
                // SQLite gives a time back with no zone, which goes out with none and is read as local time.
                LastFailureOn = lastFailureOn == null ? null : DateTime.SpecifyKind(lastFailureOn.Value, DateTimeKind.Utc),
                UsualDurationMilliseconds = MedianOf(completed),
            };

            return ResultDto<ExecutionStatsDto>.Success(stats);
        }

        // The median rather than the mean: one execution left waiting all night would drag a mean for weeks.
        private static int? MedianOf(List<Took> completed)
        {
            if (completed.Count == 0)
                return null;

            List<double> milliseconds = completed
                .Select(x => (x.CompletedAt - x.StartedOn).TotalMilliseconds)
                .Order()
                .ToList();

            int middle = milliseconds.Count / 2;
            if (milliseconds.Count % 2 == 1)
                return (int)Math.Round(milliseconds[middle]);

            return (int)Math.Round((milliseconds[middle - 1] + milliseconds[middle]) / 2);
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record Took(DateTime StartedOn, DateTime CompletedAt);
    }
}
