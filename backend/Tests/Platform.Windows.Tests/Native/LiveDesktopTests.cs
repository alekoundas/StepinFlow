using System.Drawing;
using Core.Models.Business;
using Platform.Windows.Input;
using Platform.Windows.Native;
using Platform.Windows.Windowing;

namespace Platform.Windows.Tests.Native
{
    /// <summary>
    /// Native calls against a live desktop session: real monitors, the window in front, the cursor.
    /// A headless machine has none, so the trait leaves them out unless -p:Desktop=true asks.
    /// </summary>
    [Trait("Category", "Desktop")]
    public sealed class LiveDesktopTests
    {
        private readonly WindowService _windows = new WindowService();

        // Win32 reports physical pixels only to a per-monitor aware process, as the app is.
        public LiveDesktopTests()
        {
            ScreenMetrics.EnablePerMonitorDpiAwareness();
        }

        [Fact]
        public void Every_monitor_has_bounds_and_a_dpi()
        {
            IReadOnlyList<MonitorInfo> monitors = ScreenMetrics.GetAllMonitors();

            monitors.ShouldContain(x => x.IsPrimary);
            monitors.ShouldAllBe(x => x.Bounds.Width > 0 && x.Bounds.Height > 0 && x.Dpi >= 96);
        }

        [Fact]
        public void The_window_in_front_holds_its_client_area()
        {
            TopLevelWindow front = _windows.GetWindowInFront();

            front.Handle.IsValid.ShouldBeTrue();
            Rectangle whole = _windows.GetWindowBounds(front.Handle, useClientArea: false);
            Rectangle client = _windows.GetWindowBounds(front.Handle, useClientArea: true);
            whole.IsEmpty.ShouldBeFalse();
            whole.Contains(client).ShouldBeTrue();
        }

        [Fact]
        public void The_application_windows_are_listed_with_their_processes()
        {
            IReadOnlyList<SystemWindow> windows = _windows.GetApplicationWindows();

            windows.ShouldNotBeEmpty();
            windows.ShouldAllBe(x => x.ProcessId > 0);
        }

        [Fact]
        public void The_cursor_is_on_one_of_the_monitors()
        {
            Point at = new MouseService().CursorPosition();

            ScreenMetrics.GetVirtualScreenBounds().Contains(at).ShouldBeTrue();
        }
    }
}
