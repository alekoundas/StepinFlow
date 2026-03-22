using AutoMapper;
using Business.DataService.Services;
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
            var children = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(s => s.ParentFlowStepId == request.id)
                .OrderBy(s => s.OrderNumber)
                .Select(s => new TreeNodeDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    OrderNumber = s.OrderNumber,
                    HasChildren = dbContext.FlowSteps.Any(c => c.ParentFlowStepId == s.Id)
                })
                .ToListAsync(ct);

            return children;
        }
    }
}
