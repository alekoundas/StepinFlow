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

        public WindowHandle FindWindow(WindowQuery query)
        {
            Queries.Add(query);
            return Window;
        }

        public Rectangle GetWindowBounds(WindowHandle handle, bool useClientArea)
        {
            return Bounds;
        }

        public IReadOnlyList<WindowHandle> FindWindows(WindowQuery query)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<WindowMatch> FindWindowMatches(WindowQuery query)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<SystemWindow> GetApplicationWindows()
        {
            throw new NotImplementedException();
        }

        public string? GetForegroundWindowTitle()
        {
            throw new NotImplementedException();
        }

        public bool FocusWindow(WindowHandle handle)
        {
            Actions.Add("focus");
            return Obeys;
        }

        public bool ResizeWindow(WindowHandle handle, int width, int height)
        {
            Actions.Add($"resize {width}x{height}");
            return Obeys;
        }

        public bool MoveWindow(WindowHandle handle, int x, int y)
        {
            Actions.Add($"move {x},{y}");
            return Obeys;
        }

        public bool CloseWindow(WindowHandle handle)
        {
            throw new NotImplementedException();
        }
    }
}
