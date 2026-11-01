using System.Drawing;
using Core.Enums;

namespace Core.Models.Business.AreaPointResolution
{
    public sealed class AreaResolution
    {
        public bool IsResolved { get; }
        public Rectangle Bounds { get; }
        public string? Error { get; }

        /// <summary>Why it was not found. Null when it was.</summary>
        public AreaUnresolvedEnum? Unresolved { get; }

        /// <summary>The window an application area found. None for every other type.</summary>
        public WindowHandle Window { get; }

        /// <summary>What makes this area's contents bigger or smaller: its own setting, else its parent's, else DPI.</summary>
        public ScalesWithEnum ScalesWith { get; }

        /// <summary>The DPI of the monitor holding most of the area, now.</summary>
        public int Dpi { get; }


        private AreaResolution(bool isResolved, Rectangle bounds, string? error, AreaUnresolvedEnum? unresolved, WindowHandle window, ScalesWithEnum scalesWith, int dpi)
        {
            IsResolved = isResolved;
            Bounds = bounds;
            Error = error;
            Unresolved = unresolved;
            Window = window;
            ScalesWith = scalesWith;
            Dpi = dpi;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public static AreaResolution Ok(Rectangle bounds, WindowHandle window = default, ScalesWithEnum scalesWith = ScalesWithEnum.DPI, int dpi = 96)
        {
            return new AreaResolution(true, bounds, null, null, window, scalesWith, dpi);
        }

        public static AreaResolution Fail(string error, AreaUnresolvedEnum unresolved)
        {
            return new AreaResolution(false, Rectangle.Empty, error, unresolved, WindowHandle.None, ScalesWithEnum.DPI, 96);
        }
    }
}
