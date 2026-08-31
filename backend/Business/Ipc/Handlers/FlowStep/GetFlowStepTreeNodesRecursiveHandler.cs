using Core.Helpers;
using Core.Models.Dtos;
using Core.Models.Ipc;

using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// A whole flow's steps as one tree, in one query.
    ///
    /// The lazy tree asks for a level at a time, which is right for a page you are editing. The
    /// execution page is not: you set breakpoints anywhere before a run, and a step you have not
    /// expanded to is a step you cannot break on. RootId is what makes this one query rather than
    /// one per level.
    /// </summary>
    public class GetFlowStepTreeNodesRecursiveHandler
        : IRequestHandler<GetFlowStepTreeNodesRecursiveQuery, ResultDto<IEnumerable<TreeNodeDto>>>
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepTreeNodesRecursiveHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<IEnumerable<TreeNodeDto>>> Handle(GetFlowStepTreeNodesRecursiveQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<TreeNodeDto> nodes = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.RootId == request.flowId)
                .OrderBy(x => x.OrderNumber)
                .Select(FlowStepTreeNodeProjection.Row)
                .ToListAsync(ct);

            foreach (TreeNodeDto node in nodes)
                FlowStepTreeNodeProjection.Describe(node);

            return ResultDto<IEnumerable<TreeNodeDto>>.Success(BuildTree(nodes));
        }


        // ================================================================
        // Private methods
        // ================================================================

        /// <summary>
        /// Hangs every step off its parent in memory. A step whose parent is not in the list is a
        /// root of this flow, which is exactly what the tree wants at the top level.
        /// </summary>
        private static List<TreeNodeDto> BuildTree(List<TreeNodeDto> nodes)
        {
            Dictionary<int, TreeNodeDto> nodesById = nodes.ToDictionary(x => x.EntityId);
            List<TreeNodeDto> roots = new List<TreeNodeDto>();

            foreach (TreeNodeDto node in nodes)
            {
                if (node.ParentFlowStepId != null &&
                    nodesById.TryGetValue(node.ParentFlowStepId.Value, out TreeNodeDto? parent))
                {
                    ((ICollection<TreeNodeDto>)parent.Children).Add(node);
                    continue;
                }

                roots.Add(node);
            }

            return roots;
        }
    }
}
