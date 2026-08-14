using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetLazyFlowAreaHandler : IRequestHandler<GetLazyFlowAreaQuery, ResultDto<LazyResponseDto<FlowAreaDto>>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLazyFlowAreaHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LazyResponseDto<FlowAreaDto>>> Handle(GetLazyFlowAreaQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<FlowAreaDto> flowAreaDtos = await dbContext.FlowAreas
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new FlowAreaDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Type = x.Type,

                    ParentFlowAreaId = x.ParentFlowAreaId,
                    SizingMode = x.SizingMode,
                    LocationX = x.LocationX,
                    LocationY = x.LocationY,
                    Width = x.Width,
                    Height = x.Height,
                    RatioX = x.RatioX,
                    RatioY = x.RatioY,
                    RatioWidth = x.RatioWidth,
                    RatioHeight = x.RatioHeight,

                    ProcessName = x.ProcessName,
                    TitlePattern = x.TitlePattern,
                    TitleMatchMode = x.TitleMatchMode,
                    InstanceIndex = x.InstanceIndex,
                    UseClientArea = x.UseClientArea,

                    BrowserType = x.BrowserType,
                    TabMatchValue = x.TabMatchValue,
                    TabMatchOn = x.TabMatchOn,

                    MonitorUniqueId = x.MonitorUniqueId,

                    FlowId = x.FlowId,
                    FlowStepsCount = x.FlowSteps.Count(),
                    ParentName = x.ParentFlowArea != null ? x.ParentFlowArea.Name : string.Empty,
                })
                .ToListAsync(ct);

            LazyResponseDto<FlowAreaDto> dataTableResponseDto = new LazyResponseDto<FlowAreaDto>();
            dataTableResponseDto.Data = flowAreaDtos;
            dataTableResponseDto.TotalRecords = flowAreaDtos.Count;

            return ResultDto<LazyResponseDto<FlowAreaDto>>.Success(dataTableResponseDto);
        }
    }
}
