using Core.Ports;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace Business.AreaPoint
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
            AreaResolution placed;
            switch (area.Type)
            {
                case FlowAreaTypeEnum.MONITOR:
                    placed = ResolveMonitor(area);
                    break;

                case FlowAreaTypeEnum.APPLICATION:
                    placed = ResolveApplication(area);
                    break;

                case FlowAreaTypeEnum.BROWSER_TAB:
                    return AreaResolution.Fail("Browser tab areas are not supported yet.");

                case FlowAreaTypeEnum.CUSTOM:
                default:
                    placed = ResolveCustom(area);
                    break;
            }

            if (!placed.IsResolved)
                return placed;

            return placed with
            {
                ScalesWith = ScalesWithOf(area),
                Dpi = DpiAt(placed.Bounds),
            };
        }

        public PointResolution ResolvePoint(FlowPoint point)
        {
            if (point.FlowArea == null)
                return PointResolution.Ok(new Point(point.LocationX, point.LocationY));

            AreaResolution area = ResolveArea(point.FlowArea);
            if (!area.IsResolved)
                return PointResolution.Fail(area.Error!);

            Rectangle bounds = area.Bounds;

            // Pixels were captured at the point's DPI. Inside a DPI area its contents are that much
            // bigger or smaller on another monitor, so the offset is too. Inside an AREA area RATIO
            // is the portable form, and pixels are left as they are.
            float scale = DpiScale(area, point.AuthoredDpi);

            // Both modes measure from the area's top left. Two ways to say the same thing would
            // just be a trap.
            Point resolved;
            if (point.OffsetMode == AreaSizingModeEnum.RATIO)
            {
                resolved = new Point(
                    bounds.X + (int)MathF.Floor(point.RatioX * bounds.Width),
                    bounds.Y + (int)MathF.Floor(point.RatioY * bounds.Height));
            }
            else
            {
                resolved = new Point(
                    bounds.X + Scaled(point.LocationX, scale),
                    bounds.Y + Scaled(point.LocationY, scale));
            }

            return PointResolution.Ok(Clamp(resolved, bounds));
        }



        // ================================================================
        // Private methods
        // ================================================================

        private AreaResolution ResolveMonitor(FlowArea area)
        {
            IReadOnlyList<MonitorInfo> monitors = _screenService.GetAllMonitors();

            // Empty is the primary monitor: the one choice that means the same thing on another PC.
            if (area.MonitorDeviceName.Length == 0)
            {
                MonitorInfo? primary = monitors.FirstOrDefault(x => x.IsPrimary);
                if (primary == null)
                    return AreaResolution.Fail("No primary monitor was found.");

                return AreaResolution.Ok(primary.Bounds);
            }

            MonitorInfo? monitor = monitors.FirstOrDefault(x => string.Equals(x.DeviceId, area.MonitorDeviceName, StringComparison.OrdinalIgnoreCase));
            if (monitor == null)
                return AreaResolution.Fail($"Monitor \"{area.MonitorDeviceName}\" is not connected.");

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

            // The child sits in its parent, so the parent's physics moves it: pixels written at the
            // child's authored DPI grow with a DPI parent's monitor. Its own setting only governs
            // what is inside it - a game canvas in a browser tab moves with the tab's DPI and
            // scales its templates with its own size.
            float scale = DpiScale(parent, area.AuthoredDpi);

            Rectangle bounds;
            if (area.SizingMode == AreaSizingModeEnum.RATIO)
            {
                bounds = new Rectangle(
                    parentBounds.X + (int)MathF.Floor(area.RatioX * parentBounds.Width),
                    parentBounds.Y + (int)MathF.Floor(area.RatioY * parentBounds.Height),
                    (int)MathF.Floor(area.RatioWidth * parentBounds.Width),
                    (int)MathF.Floor(area.RatioHeight * parentBounds.Height));
            }
            else
            {
                bounds = new Rectangle(
                    parentBounds.X + Scaled(area.LocationX, scale),
                    parentBounds.Y + Scaled(area.LocationY, scale),
                    Scaled(area.Width, scale),
                    Scaled(area.Height, scale));
            }

            bounds = Rectangle.Intersect(bounds, parentBounds);

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return AreaResolution.Fail($"\"{area.Name}\" falls outside the area it sits in.");

            return AreaResolution.Ok(bounds);
        }


        // Null inherits: a CUSTOM child takes its parent's, and an area with no parent is DPI.
        private static ScalesWithEnum ScalesWithOf(FlowArea area)
        {
            if (area.ScalesWith != null)
                return area.ScalesWith.Value;

            if (area.ParentFlowArea != null)
                return ScalesWithOf(area.ParentFlowArea);

            return ScalesWithEnum.DPI;
        }

        // The monitor holding the largest part of the area decides its DPI - the rule Windows uses
        // to give a window its DPI. An area on no monitor at all takes the primary's.
        private int DpiAt(Rectangle bounds)
        {
            IReadOnlyList<MonitorInfo> monitors = _screenService.GetAllMonitors();

            MonitorInfo? best = null;
            long bestOverlap = 0;
            foreach (MonitorInfo monitor in monitors)
            {
                Rectangle overlap = Rectangle.Intersect(bounds, monitor.Bounds);
                long size = (long)overlap.Width * overlap.Height;

                if (size > bestOverlap)
                {
                    bestOverlap = size;
                    best = monitor;
                }
            }

            if (best == null)
                best = monitors.FirstOrDefault(x => x.IsPrimary);

            if (best == null)
                return 96;

            return best.Dpi;
        }

        // How much bigger pixels written at authoredDpi are inside this area now. Only a DPI area's
        // contents follow the monitor, and nothing recorded leaves them as they are.
        private static float DpiScale(AreaResolution area, int authoredDpi)
        {
            if (area.ScalesWith != ScalesWithEnum.DPI || authoredDpi <= 0 || area.Dpi <= 0)
                return 1f;

            return (float)area.Dpi / authoredDpi;
        }

        private static int Scaled(int pixels, float scale)
        {
            return (int)MathF.Round(pixels * scale);
        }

        private static Point Clamp(Point point, Rectangle bounds)
        {
            return new Point(
                Math.Clamp(point.X, bounds.Left, Math.Max(bounds.Left, bounds.Right - 1)),
                Math.Clamp(point.Y, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - 1)));
        }
    }
}
