using Core.Models.Business;
using System.Drawing;

namespace Core.Ports
{
    /// <summary>
    /// Finding and driving application windows.
    ///
    /// Handles are opaque: see <see cref="WindowHandle"/>. Nothing outside the adapter may
    /// interpret one, which is why they are only ever passed straight back in.
    ///
    /// Deciding whether a window matches is <see cref="Helpers.WindowMatcherHelper"/> rather than an
    /// implementation of this, because that is a rule and not a machine.
    /// </summary>
    public interface IWindowService
    {
        /// <summary>Windows matching the query, in z-order. Empty when nothing matches.</summary>
        IReadOnlyList<WindowHandle> FindWindows(WindowQuery query);

        /// <summary>The first match, or <see cref="WindowHandle.None"/>.</summary>
        WindowHandle FindWindow(WindowQuery query);

        /// <summary>Every match with the detail a person needs to tell them apart.</summary>
        IReadOnlyList<WindowMatch> FindWindowMatches(WindowQuery query);

        /// <summary>Every visible window with a title, for a picker.</summary>
        IReadOnlyList<SystemWindow> GetApplicationWindows();

        string? GetForegroundWindowTitle();

        /// <summary>
        /// Bounds in physical pixels. The client area excludes the title bar and borders, so a
        /// stored offset means the same thing whatever chrome the window happens to have.
        /// Empty when the handle is not a live window.
        /// </summary>
        Rectangle GetWindowBounds(WindowHandle handle, bool useClientArea);

        bool FocusWindow(WindowHandle handle);

        bool ResizeWindow(WindowHandle handle, int width, int height);

        bool MoveWindow(WindowHandle handle, int x, int y);

        /// <summary>
        /// Asks the window to close, the way clicking its X does. Never waits, so an application
        /// that puts up "are you sure" cannot block the caller for ever.
        /// </summary>
        bool CloseWindow(WindowHandle handle);
    }
}
