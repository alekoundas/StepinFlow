using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetFlowLocationHandler : IRequestHandler<GetFlowLocationQuery, ResultDto<FlowLocationDto>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowLocationHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowLocationDto>> Handle(GetFlowLocationQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowLocationDto? flowLocationDto = await dbContext.FlowLocations
                .AsNoTracking()
                .Where(x => x.Id == request.id)
                .Select(x => new FlowLocationDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    LocationX = x.LocationX,
                    LocationY = x.LocationY,
                    FlowId = x.FlowId,
                    FlowStepsCount = x.FlowSteps.Count() + x.EndFlowSteps.Count(),
                })
                .FirstOrDefaultAsync(ct);

            if (flowLocationDto == null)
                return ResultDto<FlowLocationDto>.Failure("Entity doesnt exist in the Database!");

            return ResultDto<FlowLocationDto>.Success(flowLocationDto);
        }
    }
}
