using System.Drawing;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Ports;

namespace Business.AreaPoint
{
    /// <summary>
    /// The browser window holding a browser tab area's tab, found by its address.
    ///
    /// Only a window's front tab says its address, so a tab behind is found by bringing each tab of
    /// each window to the front in turn, and a window it is not in goes back to the tab it showed. A
    /// search never does that: it looks at the front tabs only, and fails rather than switch tabs
    /// behind the user's back. Focus and Launch do, because putting the tab in front is what they
    /// are for.
    /// </summary>
    public sealed class BrowserTabFinder
    {
        // Long enough for the address bar to say the address of the tab just brought to the front.
        private static readonly TimeSpan TAB_SETTLE = TimeSpan.FromMilliseconds(150);

        private readonly IWindowService _windowService;
        private readonly IBrowserTabService _browserTabService;
        private readonly TimeProvider _timeProvider;

        public BrowserTabFinder(IWindowService windowService, IBrowserTabService browserTabService, TimeProvider timeProvider)
        {
            _windowService = windowService;
            _browserTabService = browserTabService;
            _timeProvider = timeProvider;
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>The window with the area's tab in front, or <see cref="WindowHandle.None"/>.</summary>
        public WindowHandle FindInFront(FlowArea area)
        {
            foreach (WindowHandle window in WindowsOf(area))
            {
                if (IsInFront(window, area))
                    return window;
            }

            return WindowHandle.None;
        }

        /// <summary>The page of the window's front tab, in physical pixels. Empty when it cannot be read.</summary>
        public Rectangle PageBounds(WindowHandle window)
        {
            return _browserTabService.PageBounds(window);
        }

        /// <summary>How many tabs the window has. Zero when its tab strip cannot be read.</summary>
        public int TabCount(WindowHandle window)
        {
            return _browserTabService.TabCount(window);
        }

        /// <summary>
        /// The window with the area's tab, the tab brought to the front first when it was behind, and
        /// whether it was. <see cref="WindowHandle.None"/> when no tab of the browser has its address.
        /// </summary>
        public async Task<(WindowHandle Window, bool WasBehind)> BringToFrontAsync(FlowArea area, CancellationToken ct)
        {
            IReadOnlyList<WindowHandle> windows = WindowsOf(area);

            foreach (WindowHandle window in windows)
            {
                if (IsInFront(window, area))
                    return (window, false);
            }

            foreach (WindowHandle window in windows)
            {
                int front = _browserTabService.FrontTab(window);
                int count = _browserTabService.TabCount(window);

                for (int index = 0; index < count; index++)
                {
                    if (index == front || !_browserTabService.SelectTab(window, index))
                        continue;

                    await Task.Delay(TAB_SETTLE, _timeProvider, ct);

                    if (IsInFront(window, area))
                        return (window, true);
                }

                if (front >= 0)
                    _browserTabService.SelectTab(window, front);
            }

            return (WindowHandle.None, false);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Every window of the browser. Its title is its front tab's, so it says nothing about the
        // area, and a browser left unnamed would match every window on the desktop.
        private IReadOnlyList<WindowHandle> WindowsOf(FlowArea area)
        {
            if (string.IsNullOrWhiteSpace(area.ProcessName))
                return [];

            return _windowService.FindWindows(new WindowQuery { ProcessName = area.ProcessName });
        }

        private bool IsInFront(WindowHandle window, FlowArea area)
        {
            string? address = _browserTabService.FrontAddress(window);
            return address != null && TabAddressHelper.Matches(address, area.TabAddress);
        }
    }
}
