using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Marks a flow as callable. One way on purpose: a caller's SubFlowId can then never
    /// point at something that has stopped being invokable, which is what removes every stale
    /// caller case from the rest of the feature.
    /// </summary>
    public class PromoteFlowToSubFlowHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public PromoteFlowToSubFlowHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<bool>> HandleAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            Flow? flow = await dbContext.Flows.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (flow == null)
                return ResultDto<bool>.Failure("That flow no longer exists.");

            if (flow.IsSubFlow)
                return ResultDto<bool>.Success(true);

            flow.IsSubFlow = true;
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<bool>.Success(true);
        }
    }
}
