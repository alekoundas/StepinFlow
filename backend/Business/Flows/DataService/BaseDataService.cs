using Business.Validation;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Flows.DataService
{
    /// <summary>
    /// The context factory, and the rules that apply to more than one table.
    ///
    /// A public method that opens its own context is what a handler calls. It saves once, which EF
    /// already makes atomic, or opens a transaction when it saves more than once; a failure is
    /// returned before the commit, so the transaction rolls back as it is disposed. One that takes an
    /// <see cref="AppDbContext"/> runs inside the caller's, so a write that spans tables composes in
    /// one transaction; it holds no state of its own, so most are static.
    /// </summary>
    public abstract class BaseDataService
    {
        private readonly IFlowValidationService _flowValidationService;

        protected BaseDataService(IDbContextFactory<AppDbContext> dbContextFactory, IFlowValidationService flowValidationService)
        {
            DbContextFactory = dbContextFactory;
            _flowValidationService = flowValidationService;
        }

        protected IDbContextFactory<AppDbContext> DbContextFactory { get; }


        // ================================================================
        // Protected methods - rules that span the flow's tables
        // ================================================================

        // Steps, areas, points and csv columns are one namespace, because the script refers to all
        // of them by name.
        protected static async Task<HashSet<string>> TakenNamesAsync(AppDbContext dbContext, int flowId, CancellationToken ct)
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

        // A step that no longer runs under the one it reads would otherwise keep the reference and
        // act on whatever that step last produced. Cleared rather than repointed: an empty required
        // dropdown says the step needs a source, where a silently wrong value would not.
        protected static void ClearBrokenReferences(IReadOnlyList<FlowStep> steps, IReadOnlyList<FlowStepBrokenReferenceDto> brokenReferences)
        {
            foreach (FlowStepBrokenReferenceDto reference in brokenReferences)
            {
                FlowStep? step = steps.FirstOrDefault(x => x.Id == reference.FlowStepId);
                if (step == null)
                    continue;

                if (reference.IsEndReference)
                    step.FlowStepReferenceEndId = null;
                else
                    step.FlowStepReferenceId = null;
            }
        }

        // Once saved, because the rules work on ids. A flow with errors stays saved and shows them,
        // and Start is refused until they are fixed.
        protected Task<FlowValidationResultDto> ValidateAsync(AppDbContext dbContext, int flowId, CancellationToken ct)
        {
            return _flowValidationService.ValidateAsync(dbContext, flowId, ct);
        }
    }
}
