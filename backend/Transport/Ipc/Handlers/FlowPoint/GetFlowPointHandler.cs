using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class GetFlowPointHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowPointHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowPointDto>> HandleAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowPointDto? flowPointDto = await dbContext.FlowPoints
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new FlowPointDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    FlowAreaId = x.FlowAreaId,
                    OffsetMode = x.OffsetMode,
                    LocationX = x.LocationX,
                    LocationY = x.LocationY,
                    AuthoredDpi = x.AuthoredDpi,
                    RatioX = x.RatioX,
                    RatioY = x.RatioY,
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
