using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetFlowPointHandler : IRequestHandler<GetFlowPointQuery, ResultDto<FlowPointDto>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowPointHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowPointDto>> Handle(GetFlowPointQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowPointDto? flowPointDto = await dbContext.FlowPoints
                .AsNoTracking()
                .Where(x => x.Id == request.id)
                .Select(x => new FlowPointDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    LocationX = x.LocationX,
                    LocationY = x.LocationY,
                    FlowId = x.FlowId,
                    FlowStepsCount = x.FlowSteps.Count() + x.EndFlowSteps.Count(),
                })
                .FirstOrDefaultAsync(ct);

            if (flowPointDto == null)
                return ResultDto<FlowPointDto>.Failure("Entity doesnt exist in the Database!");

            return ResultDto<FlowPointDto>.Success(flowPointDto);
        }
    }
}
