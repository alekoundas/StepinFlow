using Core.Models.Dtos;

using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers.Execution
{
    /// <summary>
    /// The saved steps between two sequences, oldest first. A live execution's page keeps only so
    /// many rows, and reads the rest back from here; it works while the execution is still going,
    /// for whatever has been written so far.
    /// </summary>
    public class GetExecutionStepsHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetExecutionStepsHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<List<ExecutionStepDto>>> HandleAsync(ExecutionStepRangeDto range, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<ExecutionStepDto> steps = await dbContext.ExecutionSteps
                .AsNoTracking()
                .Where(x => x.ExecutionId == range.ExecutionId && x.Sequence >= range.FirstSequence && x.Sequence <= range.LastSequence)
                .OrderBy(x => x.Sequence)
                .Select(ExecutionStepProjection.Row)
                .ToListAsync(ct);

            ExecutionStepProjection.ClearUnwritableScores(steps);

            return ResultDto<List<ExecutionStepDto>>.Success(steps);
        }
    }
}
