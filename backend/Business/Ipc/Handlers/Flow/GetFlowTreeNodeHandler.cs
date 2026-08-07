using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetFlowTreeNodeHandler : IRequestHandler<GetFlowTreeNodeQuery, ResultDto<IEnumerable<TreeNodeDto>>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowTreeNodeHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<IEnumerable<TreeNodeDto>>> Handle(GetFlowTreeNodeQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            List<TreeNodeDto> children = await dbContext.Flows
                .AsNoTracking()
                .Where(x => x.Id == request.id)
                .OrderBy(x => x.OrderNumber)
                .Select(x => new TreeNodeDto
                {
                    EntityId = x.Id,
                    Droppable = true,
                    Draggable = false,
                    Selectable = true,
                    Leaf = false,

                    Name = x.Name,
                    flowStepType = null,
                    OrderNumber = x.OrderNumber,
                    IsFlow = true,
                    IsNew = false,

                    ParentFlowId = null,
                    ParentFlowStepId = null,
                })
                .ToListAsync(ct);

            foreach (TreeNodeDto child in children)
                child.Key = TreeNodeDto.BuildKey(child.EntityId, isFlow: true);

            return ResultDto<IEnumerable<TreeNodeDto>>.Success(children);
        }
    }
}
