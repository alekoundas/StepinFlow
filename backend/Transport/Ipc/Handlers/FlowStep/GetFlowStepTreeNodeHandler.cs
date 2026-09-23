using Core.Helpers;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class GetFlowStepTreeNodeHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepTreeNodeHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<IEnumerable<TreeNodeDto>>> HandleAsync(TreeNodeRequestDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            // Expanding the Flow node asks for its root steps, expanding a FlowStep asks for its
            // children. Both arrive here, so the caller says which one it is: matching the id
            // against both columns would let a FlowStep adopt the root steps of the Flow that
            // happens to share its id.
            IQueryable<Core.Models.Database.FlowStep> query = dto.IsFlow
                ? dbContext.FlowSteps.Where(x => x.FlowId == dto.Id && x.ParentFlowStepId == null)
                : dbContext.FlowSteps.Where(x => x.ParentFlowStepId == dto.Id);

            List<TreeNodeDto> children = await query
                .AsNoTracking()
                .OrderBy(x => x.OrderNumber)
                .Select(FlowStepTreeNodeProjection.Row)
                .ToListAsync(ct);

            // Set outside the projection so the rules live in one helper rather than as inline
            // expressions EF has to translate.
            foreach (TreeNodeDto child in children)
                FlowStepTreeNodeProjection.Describe(child);

            return ResultDto<IEnumerable<TreeNodeDto>>.Success(children);
        }
    }
}
