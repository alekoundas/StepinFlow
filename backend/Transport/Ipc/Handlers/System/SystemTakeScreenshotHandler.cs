using Business.Services.AreaPointService;
using Core.Ports;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace Transport.Ipc.Handlers
{
    public class SystemTakeScreenshotHandler
    {
        private readonly IScreenshotService _screenshotService;
        private readonly IAreaPointResolver _areaPointResolver;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public SystemTakeScreenshotHandler(
            IScreenshotService screenshotService,
            IAreaPointResolver areaPointResolver,
            IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _screenshotService = screenshotService;
            _areaPointResolver = areaPointResolver;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<byte[]>> HandleAsync(ScreenshotRequestDto dto, CancellationToken ct)
        {
            if (dto.FlowAreaId != null)
                return await CaptureArea(dto, ct);

            if (dto.CaptureVirtualScreen)
                return ResultDto<byte[]>.Success(_screenshotService.CaptureVirtualScreen(dto.FormatType, dto.JpegQuality));

            if (dto.CaptureAppWindow.Length > 0)
                return ResultDto<byte[]>.Success(_screenshotService.CaptureAppWindow(dto.CaptureAppWindow, dto.FormatType, dto.JpegQuality));

            if (dto.CaptureMonitor.Length > 0)
                return ResultDto<byte[]>.Success(_screenshotService.CaptureMonitor(dto.CaptureMonitor, dto.FormatType, dto.JpegQuality));

            Rectangle rect = new Rectangle(dto.LocationX, dto.LocationY, dto.Width, dto.Height);
            return ResultDto<byte[]>.Success(_screenshotService.Capture(rect, dto.FormatType, dto.JpegQuality));
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<ResultDto<byte[]>> CaptureArea(ScreenshotRequestDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowArea? area = await dbContext.FlowAreas
                .AsNoTracking()
                .Include(x => x.ParentFlowArea)
                .FirstOrDefaultAsync(x => x.Id == dto.FlowAreaId, ct);

            if (area == null)
                return ResultDto<byte[]>.Failure("Entity doesnt exist in the Database!");

            AreaResolution resolution = _areaPointResolver.ResolveArea(area);
            if (!resolution.IsResolved)
                return ResultDto<byte[]>.Failure(resolution.Error!);

            return ResultDto<byte[]>.Success(
                _screenshotService.CaptureResolvedArea(area, resolution.Bounds, dto.FormatType, dto.JpegQuality));
        }
    }
}
