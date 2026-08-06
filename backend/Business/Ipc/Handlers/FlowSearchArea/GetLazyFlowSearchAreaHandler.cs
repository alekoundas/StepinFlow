using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetLazyFlowSearchAreaHandler : IRequestHandler<GetLazyFlowSearchAreaQuery, ResultDto<LazyResponseDto<FlowSearchAreaDto>>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLazyFlowSearchAreaHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LazyResponseDto<FlowSearchAreaDto>>> Handle(GetLazyFlowSearchAreaQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<FlowSearchAreaDto> flowSearchAreaDtos = await dbContext.FlowSearchAreas
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new FlowSearchAreaDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Type = x.Type,
                    AppWindowName = x.AppWindowName,
                    MonitorUniqueId = x.MonitorUniqueId,
                    LocationX = x.LocationX,
                    LocationY = x.LocationY,
                    Width = x.Width,
                    Height = x.Height,
                    FlowId = x.FlowId,
                    FlowStepsCount = x.FlowSteps.Count(),
                })
                .ToListAsync(ct);

            LazyResponseDto<FlowSearchAreaDto> dataTableResponseDto = new LazyResponseDto<FlowSearchAreaDto>();
            dataTableResponseDto.Data = flowSearchAreaDtos;
            dataTableResponseDto.TotalRecords = flowSearchAreaDtos.Count;

            return ResultDto<LazyResponseDto<FlowSearchAreaDto>>.Success(dataTableResponseDto);
        }
    }
}
