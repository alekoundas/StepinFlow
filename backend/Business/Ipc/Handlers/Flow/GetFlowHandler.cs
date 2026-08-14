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

        // One query. The child projections are inlined rather than shared with the lazy grid
        // handler: EF only accepts a stored Expression at the top level of a query, so reusing one
        // would force a round trip per collection.
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

                    FlowAreas = x.FlowAreas
                        .OrderBy(a => a.Name)
                        .Select(a => new FlowAreaDto
                        {
                            Id = a.Id,
                            Name = a.Name,
                            Type = a.Type,

                            ParentFlowAreaId = a.ParentFlowAreaId,
                            SizingMode = a.SizingMode,
                            LocationX = a.LocationX,
                            LocationY = a.LocationY,
                            Width = a.Width,
                            Height = a.Height,
                            RatioX = a.RatioX,
                            RatioY = a.RatioY,
                            RatioWidth = a.RatioWidth,
                            RatioHeight = a.RatioHeight,

                            ProcessName = a.ProcessName,
                            TitlePattern = a.TitlePattern,
                            TitleMatchMode = a.TitleMatchMode,
                            InstanceIndex = a.InstanceIndex,
                            UseClientArea = a.UseClientArea,

                            BrowserType = a.BrowserType,
                            TabMatchValue = a.TabMatchValue,
                            TabMatchOn = a.TabMatchOn,

                            MonitorUniqueId = a.MonitorUniqueId,

                            FlowId = a.FlowId,
                            FlowStepsCount = a.FlowSteps.Count(),
                            ParentName = a.ParentFlowArea != null ? a.ParentFlowArea.Name : string.Empty,
                        })
                        .ToList(),

                    FlowPoints = x.FlowPoints
                        .OrderBy(l => l.Name)
                        .Select(l => new FlowPointDto
                        {
                            Id = l.Id,
                            Name = l.Name,

                            FlowAreaId = l.FlowAreaId,
                            OffsetMode = l.OffsetMode,
                            LocationX = l.LocationX,
                            LocationY = l.LocationY,
                            RatioX = l.RatioX,
                            RatioY = l.RatioY,

                            FlowId = l.FlowId,
                            FlowStepsCount = l.FlowSteps.Count() + l.EndFlowSteps.Count(),
                            FlowAreaName = l.FlowArea != null ? l.FlowArea.Name : string.Empty,
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
