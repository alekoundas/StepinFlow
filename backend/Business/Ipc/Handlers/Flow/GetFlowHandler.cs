using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetFlowHandler : IRequestHandler<GetFlowQuery, ResultDto<FlowDto>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        // Projected instead of Include + Map so the usage counts are computed by SQLite in the
        // same round trip, and no FlowStep rows are dragged along for a form that never shows them.
        public async Task<ResultDto<FlowDto>> Handle(GetFlowQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowDto? flowDto = await dbContext.Flows
                .AsNoTracking()
                .Where(x => x.Id == request.id)
                .Select(x => new FlowDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    OrderNumber = x.OrderNumber,

                    FlowSearchAreas = x.FlowSearchAreas
                        .OrderBy(a => a.Name)
                        .Select(a => new FlowSearchAreaDto
                        {
                            Id = a.Id,
                            Name = a.Name,
                            Type = a.Type,
                            AppWindowName = a.AppWindowName,
                            MonitorUniqueId = a.MonitorUniqueId,
                            LocationX = a.LocationX,
                            LocationY = a.LocationY,
                            Width = a.Width,
                            Height = a.Height,
                            FlowId = a.FlowId,
                            FlowStepsCount = a.FlowSteps.Count(),
                        })
                        .ToList(),

                    FlowLocations = x.FlowLocations
                        .OrderBy(l => l.Name)
                        .Select(l => new FlowLocationDto
                        {
                            Id = l.Id,
                            Name = l.Name,
                            LocationX = l.LocationX,
                            LocationY = l.LocationY,
                            FlowId = l.FlowId,
                            FlowStepsCount = l.FlowSteps.Count() + l.EndFlowSteps.Count(),
                        })
                        .ToList(),
                })
                .FirstOrDefaultAsync(ct);

            if (flowDto == null)
                return ResultDto<FlowDto>.Failure("Entity doesnt exist in the Database!");

            return ResultDto<FlowDto>.Success(flowDto);
        }
    }
}
