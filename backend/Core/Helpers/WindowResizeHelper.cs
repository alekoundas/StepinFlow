using Core.Enums;
using Core.Models.Database;

namespace Core.Helpers
{
    /// <summary>
    /// What a Resize Window sets: the size its area measures. Windows draws the title bar and borders
    /// thicker at a higher scaling, so the same whole window leaves a smaller inside on another PC.
    /// A browser's window is set whole, since its page reflows rather than growing with it.
    /// </summary>
    public static class WindowResizeHelper
    {
        public static bool SetsInside(FlowArea area)
        {
            return area.Type == FlowAreaTypeEnum.APPLICATION && area.IgnoreWindowBorders;
        }
    }
}
