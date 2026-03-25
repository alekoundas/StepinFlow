using AutoMapper;
using Business.DataService.Services;
using Core.Enums;
using Core.Models.Dtos;
using Core.Models.Ipc.Commands.Flow;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetFlowStepTreeNodeHandler : IRequestHandler<GetFlowStepTreeNodeQuery, IEnumerable<TreeNodeDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepTreeNodeHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<IEnumerable<TreeNodeDto>> Handle(GetFlowStepTreeNodeQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<TreeNodeDto> children = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.FlowId == request.id || x.ParentFlowStepId == request.id)
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

            return children;
        }
    }
}
