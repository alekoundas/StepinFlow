using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// The flows that invoke this one. Shown on a sub-flow so editing it is a decision rather
    /// than a surprise for whoever depends on it.
    /// </summary>
    public class GetFlowCallersHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowCallersHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<IReadOnlyList<LookupItemDto>>> HandleAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            // RootId is the flow the step lives in, which is exactly the caller.
            List<int> callerIds = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.SubFlowId == id)
                .Select(x => x.RootId)
                .Distinct()
                .ToListAsync(ct);

            List<LookupItemDto> callers = await dbContext.Flows
                .AsNoTracking()
                .Where(x => callerIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemDto
                {
                    Value = x.Id.ToString(CultureInfo.InvariantCulture),
                    Label = x.Name,
                    Description = x.IsSubFlow ? "sub-flow" : "flow",
                })
                .ToListAsync(ct);

            return ResultDto<IReadOnlyList<LookupItemDto>>.Success(callers);
        }
    }
}
