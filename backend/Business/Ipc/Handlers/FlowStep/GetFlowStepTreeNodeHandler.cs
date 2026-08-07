using Core.Enums;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetFlowStepTreeNodeHandler : IRequestHandler<GetFlowStepTreeNodeQuery, ResultDto<IEnumerable<TreeNodeDto>>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepTreeNodeHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<IEnumerable<TreeNodeDto>>> Handle(GetFlowStepTreeNodeQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            // Expanding the Flow node asks for its root steps, expanding a FlowStep asks for its
            // children. Both arrive here, so the caller says which one it is: matching the id
            // against both columns would let a FlowStep adopt the root steps of the Flow that
            // happens to share its id.
            IQueryable<Core.Models.Database.FlowStep> query = request.dto.IsFlow
                ? dbContext.FlowSteps.Where(x => x.FlowId == request.dto.Id && x.ParentFlowStepId == null)
                : dbContext.FlowSteps.Where(x => x.ParentFlowStepId == request.dto.Id);

            List<TreeNodeDto> children = await query
                .AsNoTracking()
                .OrderBy(x => x.OrderNumber)
                .Select(x => new TreeNodeDto
                {
                    EntityId = x.Id,
                    Droppable = x.FlowStepType == FlowStepTypeEnum.FAILURE
                        || x.FlowStepType == FlowStepTypeEnum.SUCCESS
                        || x.FlowStepType == FlowStepTypeEnum.LOOP,
                    Draggable = true,
                    Selectable = true,
                    Leaf = x.FlowStepType != FlowStepTypeEnum.FAILURE
                        && x.FlowStepType != FlowStepTypeEnum.SUCCESS
                        && x.FlowStepType != FlowStepTypeEnum.LOOP,


                    Name = x.Name,
                    flowStepType = x.FlowStepType,
                    OrderNumber = x.OrderNumber,
                    IsFlow = false,
                    IsNew = false,

                    ParentFlowId = x.FlowId,
                    ParentFlowStepId = x.ParentFlowStepId,
                })
                .ToListAsync(ct);

            foreach (TreeNodeDto child in children)
                child.Key = TreeNodeDto.BuildKey(child.EntityId, isFlow: false);

            return ResultDto<IEnumerable<TreeNodeDto>>.Success(children);
        }
    }
}
