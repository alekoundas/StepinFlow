using AutoMapper;
using Business.DataService.Services;
using Core.Models.Dtos;
using Core.Models.Ipc.Commands.Flow;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetFlowTreeNodeHandler : IRequestHandler<GetFlowTreeNodeQuery, IEnumerable<TreeNodeDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowTreeNodeHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<IEnumerable<TreeNodeDto>> Handle(GetFlowTreeNodeQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var children = await dbContext.Flows
                .AsNoTracking()
                .Where(x => x.Id == request.id)
                .OrderBy(x => x.OrderNumber)
                .Select(x => new TreeNodeDto
                {
                    Key = x.Id.ToString(),
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

            return children;
        }
    }
}
