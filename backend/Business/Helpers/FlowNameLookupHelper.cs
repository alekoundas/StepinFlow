using Core.Enums;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Helpers
{
    /// <summary>
    /// The names already in use in a flow: steps, areas, points and csv columns.
    /// </summary>
    public static class FlowNameLookupHelper
    {
        public static async Task<HashSet<string>> TakenAsync(AppDbContext dbContext, int flowId, CancellationToken ct)
        {
            List<string> names = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.RootId == flowId)
                .Where(x => x.FlowStepType != FlowStepTypeEnum.SUCCESS && x.FlowStepType != FlowStepTypeEnum.FAILURE)
                .Select(x => x.Name)
                .Concat(dbContext.FlowAreas
                    .AsNoTracking()
                    .Where(x => x.FlowId == flowId)
                    .Select(x => x.Name))
                .Concat(dbContext.FlowPoints
                    .AsNoTracking()
                    .Where(x => x.FlowId == flowId)
                    .Select(x => x.Name))
                .Concat(dbContext.FlowCsvColumns
                    .AsNoTracking()
                    .Where(x => x.FlowId == flowId)
                    .Select(x => x.Name))
                .ToListAsync(ct);

            return new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        }
    }
}
