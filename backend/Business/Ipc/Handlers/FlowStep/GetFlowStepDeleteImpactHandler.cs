using Core.Models.Dtos;
using Core.Models.Ipc;

using DataAccess;

using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// Counts what a delete would take, so the question asked before it is the true one.
    ///
    /// Walked in memory rather than asked of the database recursively: a flow is tens of steps, and
    /// three columns of all of them costs less than teaching sqlite to recurse through EF.
    /// </summary>
    public class GetFlowStepDeleteImpactHandler : IRequestHandler<GetFlowStepDeleteImpactQuery, ResultDto<FlowStepDeleteImpactDto>>
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepDeleteImpactHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowStepDeleteImpactDto>> Handle(GetFlowStepDeleteImpactQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            int? rootId = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.Id == request.id)
                .Select(x => (int?)x.RootId)
                .FirstOrDefaultAsync(ct);

            if (rootId == null)
                return ResultDto<FlowStepDeleteImpactDto>.Failure("Entity doesnt exist in the Database!");

            List<StepLink> links = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.RootId == rootId.Value)
                .Select(x => new StepLink(x.Id, x.ParentFlowStepId, x.FlowStepReferenceId, x.FlowStepReferenceEndId))
                .ToListAsync(ct);

            HashSet<int> removed = Removed(links, request.id);

            // Only the ones that outlive the delete. A reference from inside the subtree goes with
            // everything else, so counting it would be counting a problem that cannot happen.
            int referencing = links.Count(x =>
                !removed.Contains(x.Id) &&
                ((x.ReferenceId != null && removed.Contains(x.ReferenceId.Value)) ||
                 (x.ReferenceEndId != null && removed.Contains(x.ReferenceEndId.Value))));

            return ResultDto<FlowStepDeleteImpactDto>.Success(new FlowStepDeleteImpactDto
            {
                DescendantCount = removed.Count - 1,
                ReferencingStepCount = referencing,
            });
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The step and everything under it, to any depth, which is what the database cascade takes.
        private static HashSet<int> Removed(List<StepLink> links, int id)
        {
            ILookup<int?, StepLink> byParent = links.ToLookup(x => x.ParentId);

            HashSet<int> removed = new HashSet<int> { id };
            Queue<int> pending = new Queue<int>();
            pending.Enqueue(id);

            while (pending.Count > 0)
            {
                int current = pending.Dequeue();

                foreach (StepLink child in byParent[current])
                {
                    // A cycle would loop forever, and the set is the guard against it.
                    if (removed.Add(child.Id))
                        pending.Enqueue(child.Id);
                }
            }

            return removed;
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record StepLink(int Id, int? ParentId, int? ReferenceId, int? ReferenceEndId);
    }
}
