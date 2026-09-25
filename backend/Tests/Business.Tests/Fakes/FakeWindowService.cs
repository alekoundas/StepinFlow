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
            throw new NotImplementedException();
        }

        public bool ResizeWindow(WindowHandle handle, int width, int height)
        {
            throw new NotImplementedException();
        }

        public bool MoveWindow(WindowHandle handle, int x, int y)
        {
            throw new NotImplementedException();
        }

        public bool CloseWindow(WindowHandle handle)
        {
            throw new NotImplementedException();
        }
    }
}
