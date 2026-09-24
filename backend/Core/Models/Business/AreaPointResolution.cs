using System.Drawing;

using Core.Enums;

namespace Core.Models.Business
{
    public sealed record AreaResolution(bool IsResolved, Rectangle Bounds, string? Error)
    {
        /// <summary>What makes this area's contents bigger or smaller: its own setting, else its parent's, else DPI.</summary>
        public ScalesWithEnum ScalesWith { get; init; } = ScalesWithEnum.DPI;

        /// <summary>The DPI of the monitor holding most of the area, now.</summary>
        public int Dpi { get; init; } = 96;

        public static AreaResolution Ok(Rectangle bounds) => new AreaResolution(true, bounds, null);
        public static AreaResolution Fail(string error) => new AreaResolution(false, Rectangle.Empty, error);
    }

    public sealed record PointResolution(bool IsResolved, Point Point, string? Error)
    {
        public static PointResolution Ok(Point point) => new PointResolution(true, point, null);
        public static PointResolution Fail(string error) => new PointResolution(false, Point.Empty, error);
    }
}
