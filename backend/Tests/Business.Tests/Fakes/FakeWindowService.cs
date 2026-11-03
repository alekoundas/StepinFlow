using System.Drawing;

using Core.Models.Business;
using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>One window, or none: what an application area needs to resolve.</summary>
    public sealed class FakeWindowService : IWindowService
    {
        public WindowHandle Window { get; set; } = WindowHandle.None;
        public Rectangle Bounds { get; set; }
        public List<WindowQuery> Queries { get; } = new List<WindowQuery>();
        public List<string> Actions { get; } = new List<string>();

        // What focus, resize and move report: false is a window that refused.
        public bool Obeys { get; set; } = true;

        // A maximised or fixed-size window: it says it resized and keeps its size.
        public bool KeepsItsSize { get; set; }

        public WindowHandle FindWindow(WindowQuery query)
        {
            Queries.Add(query);
            return Window;
        }

        public Rectangle GetWindowBounds(WindowHandle handle, bool useClientArea)
        {
            return Bounds;
        }

        // Every window of a browser, front first.
        public List<WindowHandle> Windows { get; } = new List<WindowHandle>();

        public IReadOnlyList<WindowHandle> FindWindows(WindowQuery query)
        {
            Queries.Add(query);
            return Windows;
        }

        public IReadOnlyList<WindowMatch> FindWindowMatches(WindowQuery query)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<SystemWindow> GetApplicationWindows()
        {
            throw new NotImplementedException();
        }

        // What a press lands on and what keys go to.
        public TopLevelWindow Pointed { get; set; } = TopLevelWindow.None;
        public TopLevelWindow InFront { get; set; } = TopLevelWindow.None;
        public int WindowProcessId { get; set; }

        public TopLevelWindow GetWindowAt(Point point)
        {
            return Pointed;
        }

        public TopLevelWindow GetWindowInFront()
        {
            return InFront;
        }

        public int GetProcessId(WindowHandle handle)
        {
            return WindowProcessId;
        }

        public bool FocusWindow(WindowHandle handle)
        {
            Actions.Add("focus");
            return Obeys;
        }

        public bool ResizeWindow(WindowHandle handle, int width, int height)
        {
            Actions.Add($"resize {width}x{height}");

            if (Obeys && !KeepsItsSize)
                Bounds = new Rectangle(Bounds.X, Bounds.Y, width, height);

            return Obeys;
        }

        public bool MoveWindow(WindowHandle handle, int x, int y)
        {
            Actions.Add($"move {x},{y}");
            return Obeys;
        }

        // A window that goes when asked, or one that stays, asking whether to save. Closed is also what
        // ending its program does.
        public bool ClosesWhenAsked { get; set; } = true;
        public bool IsClosed { get; set; }

        public bool CloseWindow(WindowHandle handle)
        {
            Actions.Add("close");

            if (Obeys && ClosesWhenAsked)
                IsClosed = true;

            return Obeys;
        }

        public bool IsWindowOpen(WindowHandle handle)
        {
            return handle.IsValid && !IsClosed;
        }
    }
}
