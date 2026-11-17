using System.Drawing;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Ports;

namespace Business.Recording.Setup
{
    /// <summary>
    /// The main window set to the size the flow gives it, before anything is recorded, so the
    /// template images and points are taken at the size every execution sets.
    /// </summary>
    public static class RecordingWindowSize
    {
        /// <summary>
        /// Resizes the window as the Resize Window step does, its inside when the area leaves out the
        /// title bar and borders, and measures it after. Null when it took that size; otherwise what it did.
        /// </summary>
        public static string? Apply(IWindowService windowService, WindowHandle window, FlowArea area, int width, int height)
        {
            bool isInside = WindowResizeHelper.SetsInside(area);
            if (!windowService.ResizeWindow(window, width, height, isInside))
                return $"The window would not resize to {width} x {height}.";

            // A maximised, full-screen or fixed-size window says yes and keeps its size.
            Rectangle bounds = windowService.GetWindowBounds(window, isInside);
            if (bounds.Width == width && bounds.Height == height)
                return null;

            if (isInside)
                return $"The window's inside stayed {bounds.Width} x {bounds.Height} rather than {width} x {height}.";

            return $"The window stayed {bounds.Width} x {bounds.Height} rather than {width} x {height}.";
        }
    }
}
