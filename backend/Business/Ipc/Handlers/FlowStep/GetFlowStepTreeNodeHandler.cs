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

            // request.id is a FlowStep id, so only ParentFlowStepId may be matched against it.
            // Matching FlowId too would adopt the root steps of the Flow that happens to share the id.
            List<TreeNodeDto> children = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.ParentFlowStepId == request.id)
                .OrderBy(x => x.OrderNumber)
                .Select(x => new TreeNodeDto
                {
                    Key = x.Id.ToString(),
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

            return ResultDto<IEnumerable<TreeNodeDto>>.Success(children);
        }
    }
}
