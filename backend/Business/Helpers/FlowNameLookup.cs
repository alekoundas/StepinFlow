using Core.Enums;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Helpers
{
    /// <summary>
    /// The names already in use in a flow - steps, areas, points and csv columns, one namespace.
    ///
    /// Shared between the create paths so a step saved one at a time and a whole draft saved at
    /// once cannot disagree about what counts as taken.
    /// </summary>
    public static class FlowNameLookup
    {
        public static async Task<HashSet<string>> TakenAsync(AppDbContext dbContext, int flowId, CancellationToken ct)
        {
            // Success and Failure rows are excluded, matching the validator: they are structural,
            // nothing refers to them by name, and reserving those two words would rename a step the
            // validator would then never explain.
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
