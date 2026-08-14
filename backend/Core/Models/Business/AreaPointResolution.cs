using System.Drawing;

namespace Core.Models.Business
{
    public sealed record AreaResolution(bool IsResolved, Rectangle Bounds, string? Error)
    {
        public static AreaResolution Ok(Rectangle bounds) => new AreaResolution(true, bounds, null);
        public static AreaResolution Fail(string error) => new AreaResolution(false, Rectangle.Empty, error);
    }

    public sealed record PointResolution(bool IsResolved, Point Point, string? Error)
    {
        public static PointResolution Ok(Point point) => new PointResolution(true, point, null);
        public static PointResolution Fail(string error) => new PointResolution(false, Point.Empty, error);
    }
}
