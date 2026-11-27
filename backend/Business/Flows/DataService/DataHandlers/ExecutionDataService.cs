using System.Linq.Expressions;

using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Flows.DataService.DataHandlers
{
    /// <summary>
    /// Deletes executions by marking them: gone from every query at once, in one short write,
    /// however many steps they have. Their steps, rows and screenshots folders go when the person
    /// presses the cleanup on Home. Which execution is still going is the handler's to ask.
    /// </summary>
    public sealed class ExecutionDataService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public ExecutionDataService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<ResultDto<bool>> DeleteAsync(int executionId, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            int deleted = await DeleteWhereAsync(dbContext, x => x.Id == executionId, ct);
            if (deleted == 0)
                return ResultDto<bool>.Failure("That execution is not there any more.");

            return ResultDto<bool>.Success(true);
        }

        /// <summary>Every execution of one flow but the one given, 0 for none. How many went.</summary>
        public async Task<ResultDto<int>> DeleteForFlowAsync(int flowId, int exceptExecutionId, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            int deleted = await DeleteWhereAsync(dbContext, x => x.FlowId == flowId && x.Id != exceptExecutionId, ct);

            return ResultDto<int>.Success(deleted);
        }

        /// <summary>The same, inside the caller's transaction: a flow's delete takes its executions with it.</summary>
        public static Task<int> DeleteWhereAsync(AppDbContext dbContext, Expression<Func<Execution, bool>> which, CancellationToken ct)
        {
            return dbContext.Executions
                .Where(x => !x.IsDeleted)
                .Where(which)
                .ExecuteUpdateAsync(x => x.SetProperty(execution => execution.IsDeleted, true), ct);
        }
    }
}
