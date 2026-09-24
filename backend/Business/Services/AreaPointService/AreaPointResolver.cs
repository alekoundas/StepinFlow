using Core.Helpers;
using Core.Ports;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace Business.Services.AreaPointService
{
    public sealed class AreaPointResolver : IAreaPointResolver
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IWindowService _windowService;
        private readonly IScreenService _screenService;

        public AreaPointResolver(IDbContextFactory<AppDbContext> dbContextFactory, IWindowService windowService, IScreenService screenService)
        {
            _dbContextFactory = dbContextFactory;
            _windowService = windowService;
            _screenService = screenService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<AreaResolution> ResolveAreaAsync(int flowAreaId, CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowArea? area = await dbContext.FlowAreas
                .AsNoTracking()
                .Include(x => x.ParentFlowArea)
                .FirstOrDefaultAsync(x => x.Id == flowAreaId, ct);

            if (area == null)
                return AreaResolution.Fail("The area no longer exists.");

            return ResolveArea(area);
        }

        public async Task<PointResolution> ResolvePointAsync(int flowPointId, CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowPoint? point = await dbContext.FlowPoints
                .AsNoTracking()
                .Include(x => x.FlowArea)
                .ThenInclude(x => x!.ParentFlowArea)
                .FirstOrDefaultAsync(x => x.Id == flowPointId, ct);

            if (point == null)
                return PointResolution.Fail("The point no longer exists.");

            return ResolvePoint(point);
        }

        public AreaResolution ResolveArea(FlowArea area)
        {
            switch (area.Type)
            {
                case FlowAreaTypeEnum.MONITOR:
                    return ResolveMonitor(area);

                case FlowAreaTypeEnum.APPLICATION:
                    return ResolveApplication(area);

                case FlowAreaTypeEnum.BROWSER_TAB:
                    return AreaResolution.Fail("Browser tab areas are not supported yet.");

                case FlowAreaTypeEnum.CUSTOM:
                default:
                    return ResolveCustom(area);
            }
        }

        public PointResolution ResolvePoint(FlowPoint point)
        {
            if (point.FlowArea == null)
                return PointResolution.Ok(new Point(point.LocationX, point.LocationY));

            AreaResolution area = ResolveArea(point.FlowArea);
            if (!area.IsResolved)
                return PointResolution.Fail(area.Error!);

            Rectangle bounds = area.Bounds;

            // Both modes measure from the area's top left. Two ways to say the same thing would
            // just be a trap.
            Point resolved = point.OffsetMode == AreaSizingModeEnum.RATIO
                ? new Point(
                    bounds.X + (int)MathF.Floor(point.RatioX * bounds.Width),
                    bounds.Y + (int)MathF.Floor(point.RatioY * bounds.Height))
                : Offset(new Point(bounds.X, bounds.Y), point.LocationX, point.LocationY);

            return PointResolution.Ok(Clamp(resolved, bounds));
        }



        // ================================================================
        // Private methods
        // ================================================================

        private AreaResolution ResolveMonitor(FlowArea area)
        {
            MonitorInfo? monitor = MonitorHelper.Find(_screenService.GetAllMonitors(), area.MonitorDeviceName);

            if (monitor == null)
            {
                return AreaResolution.Fail(area.MonitorDeviceName.Length == 0
                    ? "No primary monitor was found."
                    : $"Monitor \"{area.MonitorDeviceName}\" is not connected.");
            }

            return AreaResolution.Ok(monitor.Bounds);
        }

        private AreaResolution ResolveApplication(FlowArea area)
        {
            WindowQuery query = new WindowQuery
            {
                ProcessName = area.ProcessName,
                TitlePattern = area.TitlePattern,
                TitleMatchMode = area.TitleMatchMode,
                UseClientArea = area.UseClientArea,
            };

            WindowHandle hwnd = _windowService.FindWindow(query);
            if (!hwnd.IsValid)
                return AreaResolution.Fail($"No window matches \"{area.Name}\".");

            Rectangle bounds = _windowService.GetWindowBounds(hwnd, area.UseClientArea);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return AreaResolution.Fail($"\"{area.Name}\" was found but has no visible area.");

            return AreaResolution.Ok(bounds);
        }

        private AreaResolution ResolveCustom(FlowArea area)
        {
            if (area.ParentFlowAreaId == null || area.ParentFlowArea == null)
            {
                Rectangle absolute = new Rectangle(area.LocationX, area.LocationY, area.Width, area.Height);

                if (absolute.Width <= 0 || absolute.Height <= 0)
                    return AreaResolution.Fail($"\"{area.Name}\" has no size.");

                return AreaResolution.Ok(absolute);
            }

            AreaResolution parent = ResolveArea(area.ParentFlowArea);
            if (!parent.IsResolved)
                return AreaResolution.Fail(parent.Error!);

            Rectangle parentBounds = parent.Bounds;

            Rectangle bounds = area.SizingMode == AreaSizingModeEnum.RATIO
                ? new Rectangle(
                    parentBounds.X + (int)MathF.Floor(area.RatioX * parentBounds.Width),
                    parentBounds.Y + (int)MathF.Floor(area.RatioY * parentBounds.Height),
                    (int)MathF.Floor(area.RatioWidth * parentBounds.Width),
                    (int)MathF.Floor(area.RatioHeight * parentBounds.Height))
                : new Rectangle(parentBounds.X + area.LocationX, parentBounds.Y + area.LocationY, area.Width, area.Height);

            bounds = Rectangle.Intersect(bounds, parentBounds);

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return AreaResolution.Fail($"\"{area.Name}\" falls outside the area it sits in.");

            return AreaResolution.Ok(bounds);
        }


        private static Point Offset(Point origin, int dx, int dy) => new Point(origin.X + dx, origin.Y + dy);

        private static Point Clamp(Point point, Rectangle bounds) => new Point(
            Math.Clamp(point.X, bounds.Left, Math.Max(bounds.Left, bounds.Right - 1)),
            Math.Clamp(point.Y, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - 1)));
    }
}
