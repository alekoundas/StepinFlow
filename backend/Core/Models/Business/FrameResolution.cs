using System.Drawing;

namespace Core.Models.Business
{
    public sealed record AreaResolution(bool IsResolved, Rectangle Bounds, string? Error)
    {
        public static AreaResolution Ok(Rectangle bounds) => new AreaResolution(true, bounds, null);
        public static AreaResolution Fail(string error) => new AreaResolution(false, Rectangle.Empty, error);
    }

    public sealed record LocationResolution(bool IsResolved, Point Point, string? Error)
    {
        public static LocationResolution Ok(Point point) => new LocationResolution(true, point, null);
        public static LocationResolution Fail(string error) => new LocationResolution(false, Point.Empty, error);
    }
}
