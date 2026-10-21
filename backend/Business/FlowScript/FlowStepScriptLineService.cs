using Business.FlowScript.Text;
using Core.Helpers;
using Core.Models;
using Core.Models.Database;
using Core.Models.Dtos.FlowScript;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.FlowScript
{
    /// <summary>
    /// A step's form shows the line the step will be in its flow's script, printed by the printer
    /// an export uses, so the two never disagree, and the name a step named by its settings gets.
    /// </summary>
    public sealed class FlowStepScriptLineService : IFlowStepScriptLineService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IPrinter _printer;

        public FlowStepScriptLineService(IDbContextFactory<AppDbContext> dbContextFactory, IPrinter printer)
        {
            _dbContextFactory = dbContextFactory;
            _printer = printer;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<FlowStepScriptLineDto> WriteAsync(FlowStep step, CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            step.FlowArea = await FindAsync(dbContext.FlowAreas, step.FlowAreaId, ct);
            step.FlowPoint = await FindAsync(dbContext.FlowPoints, step.FlowPointId, ct);
            step.FlowPointEnd = await FindAsync(dbContext.FlowPoints, step.FlowPointEndId, ct);
            step.FlowStepReference = await FindAsync(dbContext.FlowSteps, step.FlowStepReferenceId, ct);
            step.FlowStepReferenceEnd = await FindAsync(dbContext.FlowSteps, step.FlowStepReferenceEndId, ct);
            step.SubFlow = await FindAsync(dbContext.Flows, step.SubFlowId, ct);

            string subFlowPath = string.Empty;
            if (step.SubFlow != null)
                subFlowPath = FlowScriptExporter.SubFlowFileName(step.SubFlow.Name);

            string name = step.Name;
            if (FlowStepNameHelper.IsNamedBySettings(step.FlowStepType))
                name = FlowStepNameHelper.NameOf(step);

            return new FlowStepScriptLineDto
            {
                Script = _printer.WriteStep(step, subFlowPath),
                Name = name,
                AreaName = step.FlowArea?.Name,
                PointName = step.FlowPoint?.Name,
                PointEndName = step.FlowPointEnd?.Name,
                ReferenceName = step.FlowStepReference?.Name,
                ReferenceEndName = step.FlowStepReferenceEnd?.Name,
                SubFlowName = step.SubFlow?.Name,
            };
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static async Task<T?> FindAsync<T>(DbSet<T> rows, int? id, CancellationToken ct) where T : BaseDbModel
        {
            if (id == null)
                return null;

            return await rows.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        }
    }
}
