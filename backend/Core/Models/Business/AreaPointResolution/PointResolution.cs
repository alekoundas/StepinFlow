using System.Drawing;

namespace Core.Models.Business.AreaPointResolution
{
    public sealed class PointResolution
    {
        public bool IsResolved { get; set; }
        public Point Point { get; set; }
        public string? Error { get; set; }


        public PointResolution(bool isResolved, Point point, string? error)
        {
            IsResolved = isResolved;
            Point = point;
            Error = error;
        }


        // ================================================================
        // Public methods
        // ================================================================
        public static PointResolution Ok(Point point)
        {
            return new PointResolution(true, point, null);
        }

        public static PointResolution Fail(string error)
        {
            return new PointResolution(false, Point.Empty, error);
        }
    }
}
