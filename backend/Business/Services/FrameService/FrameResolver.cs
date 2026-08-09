using Business.Helpers;
using Business.Services.ScreenshotService;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace Business.Services.FrameService
{
    public sealed class FrameResolver : IFrameResolver
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public FrameResolver(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<AreaResolution> ResolveAreaAsync(int flowSearchAreaId, CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowSearchArea? area = await dbContext.FlowSearchAreas
                .AsNoTracking()
                .Include(x => x.ParentFlowSearchArea)
                .FirstOrDefaultAsync(x => x.Id == flowSearchAreaId, ct);

            if (area == null)
                return AreaResolution.Fail("The area no longer exists.");

            return ResolveArea(area);
        }

        public async Task<LocationResolution> ResolveLocationAsync(int flowLocationId, CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowLocation? location = await dbContext.FlowLocations
                .AsNoTracking()
                .Include(x => x.FlowSearchArea)
                .ThenInclude(x => x!.ParentFlowSearchArea)
                .FirstOrDefaultAsync(x => x.Id == flowLocationId, ct);

            if (location == null)
                return LocationResolution.Fail("The location no longer exists.");

            return ResolveLocation(location);
        }

        public AreaResolution ResolveArea(FlowSearchArea area)
        {
            switch (area.Type)
            {
                case FlowSearchAreaTypeEnum.MONITOR:
                    return ResolveMonitor(area);

                case FlowSearchAreaTypeEnum.APPLICATION:
                    return ResolveApplication(area);

                case FlowSearchAreaTypeEnum.BROWSER_TAB:
                    return AreaResolution.Fail("Browser tab areas are not supported yet.");

                case FlowSearchAreaTypeEnum.CUSTOM:
                default:
                    return ResolveCustom(area);
            }
        }

        public LocationResolution ResolveLocation(FlowLocation location)
        {
            if (location.FlowSearchArea == null)
                return LocationResolution.Ok(new Point(location.LocationX, location.LocationY));

            AreaResolution frame = ResolveArea(location.FlowSearchArea);
            if (!frame.IsResolved)
                return LocationResolution.Fail(frame.Error!);

            Rectangle bounds = frame.Bounds;

            // RATIO is always measured from the frame's top left, so the anchor only applies to
            // pixel offsets. Two ways to say the same thing would just be a trap.
            Point point = location.OffsetMode == AreaSizingModeEnum.RATIO
                ? new Point(
                    bounds.X + (int)MathF.Floor(location.RatioX * bounds.Width),
                    bounds.Y + (int)MathF.Floor(location.RatioY * bounds.Height))
                : Offset(AnchorOf(bounds, location.Anchor), location.LocationX, location.LocationY);

            return LocationResolution.Ok(Clamp(point, bounds));
        }



        // ================================================================
        // Private methods
        // ================================================================

        private static AreaResolution ResolveMonitor(FlowSearchArea area)
        {
            MonitorInfo? monitor = ScreenHelper.GetAllMonitors()
                .FirstOrDefault(x => string.Equals(x.DeviceId, area.MonitorUniqueId, StringComparison.OrdinalIgnoreCase));

            if (monitor == null)
                return AreaResolution.Fail($"Monitor \"{area.MonitorUniqueId}\" is not connected.");

            return AreaResolution.Ok(monitor.Bounds);
        }

        private static AreaResolution ResolveApplication(FlowSearchArea area)
        {
            WindowQuery query = new WindowQuery
            {
                ProcessName = area.ProcessName,
                TitlePattern = area.TitlePattern,
                TitleMatchMode = area.TitleMatchMode,
                InstanceIndex = area.InstanceIndex,
                UseClientArea = area.UseClientArea,
            };

            IntPtr hwnd = AppWindowHelper.FindWindow(query);
            if (hwnd == IntPtr.Zero)
                return AreaResolution.Fail($"No window matches \"{area.Name}\".");

            Rectangle bounds = AppWindowHelper.GetWindowBounds(hwnd, area.UseClientArea);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return AreaResolution.Fail($"\"{area.Name}\" was found but has no visible area.");

            return AreaResolution.Ok(bounds);
        }

        private AreaResolution ResolveCustom(FlowSearchArea area)
        {
            if (area.ParentFlowSearchAreaId == null || area.ParentFlowSearchArea == null)
            {
                Rectangle absolute = new Rectangle(area.LocationX, area.LocationY, area.Width, area.Height);

                if (absolute.Width <= 0 || absolute.Height <= 0)
                    return AreaResolution.Fail($"\"{area.Name}\" has no size.");

                return AreaResolution.Ok(absolute);
            }

            AreaResolution parent = ResolveArea(area.ParentFlowSearchArea);
            if (!parent.IsResolved)
                return AreaResolution.Fail(parent.Error!);

            Rectangle frame = parent.Bounds;

            Rectangle bounds = area.SizingMode == AreaSizingModeEnum.RATIO
                ? new Rectangle(
                    frame.X + (int)MathF.Floor(area.RatioX * frame.Width),
                    frame.Y + (int)MathF.Floor(area.RatioY * frame.Height),
                    (int)MathF.Floor(area.RatioWidth * frame.Width),
                    (int)MathF.Floor(area.RatioHeight * frame.Height))
                : new Rectangle(frame.X + area.LocationX, frame.Y + area.LocationY, area.Width, area.Height);

            bounds = Rectangle.Intersect(bounds, frame);

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return AreaResolution.Fail($"\"{area.Name}\" falls outside its frame.");

            return AreaResolution.Ok(bounds);
        }

        private static Point AnchorOf(Rectangle bounds, AnchorTypeEnum anchor) => anchor switch
        {
            AnchorTypeEnum.TOP_RIGHT => new Point(bounds.Right, bounds.Top),
            AnchorTypeEnum.BOTTOM_LEFT => new Point(bounds.Left, bounds.Bottom),
            AnchorTypeEnum.BOTTOM_RIGHT => new Point(bounds.Right, bounds.Bottom),
            AnchorTypeEnum.CENTER => new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2),
            _ => new Point(bounds.Left, bounds.Top),
        };

        private static Point Offset(Point origin, int dx, int dy) => new Point(origin.X + dx, origin.Y + dy);

        private static Point Clamp(Point point, Rectangle bounds) => new Point(
            Math.Clamp(point.X, bounds.Left, Math.Max(bounds.Left, bounds.Right - 1)),
            Math.Clamp(point.Y, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - 1)));
    }
}
