using Business.Services.AreaPointService;
using Core.Ports;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class GetFlowAreaPreviewHandler
    {
        private readonly IAreaPointResolver _areaPointResolver;
        private readonly IScreenshotService _screenshotService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowAreaPreviewHandler(
            IAreaPointResolver areaPointResolver,
            IScreenshotService screenshotService,
            IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _areaPointResolver = areaPointResolver;
            _screenshotService = screenshotService;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowAreaPreviewDto>> HandleAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowArea? area = await dbContext.FlowAreas
                .AsNoTracking()
                .Include(x => x.ParentFlowArea)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (area == null)
                return ResultDto<FlowAreaPreviewDto>.Failure("Entity doesnt exist in the Database!");

            AreaResolution resolution = _areaPointResolver.ResolveArea(area);

            if (!resolution.IsResolved)
            {
                return ResultDto<FlowAreaPreviewDto>.Success(new FlowAreaPreviewDto
                {
                    IsResolved = false,
                    ErrorMessage = resolution.Error,
                });
            }

            return ResultDto<FlowAreaPreviewDto>.Success(new FlowAreaPreviewDto
            {
                IsResolved = true,
                LocationX = resolution.Bounds.X,
                LocationY = resolution.Bounds.Y,
                Width = resolution.Bounds.Width,
                Height = resolution.Bounds.Height,
                Dpi = resolution.Dpi,
                Screenshot = _screenshotService.CaptureResolvedArea(area, resolution.Bounds, ScreenshotFormatEnum.JPEG, 85),
            });
        }
    }
}
