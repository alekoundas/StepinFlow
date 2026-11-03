using System.Drawing;
using Core.Models.Business;
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
        /// Resizes the whole window, title bar and borders included, as the Resize Window step
        /// does, and measures it after. Null when it took that size; otherwise what it did.
        /// </summary>
        public static string? Apply(IWindowService windowService, WindowHandle window, int width, int height)
        {
            if (!windowService.ResizeWindow(window, width, height))
                return $"The window would not resize to {width} x {height}.";

            // A maximised, full-screen or fixed-size window says yes and keeps its size.
            Rectangle bounds = windowService.GetWindowBounds(window, false);
            if (bounds.Width != width || bounds.Height != height)
                return $"The window stayed {bounds.Width} x {bounds.Height} rather than {width} x {height}.";

            return null;
        }
    }
}
