using System.Drawing;

using Core.Models.Business;
using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>Browser windows, each with its tabs' addresses and the one in front, and the page that tab shows.</summary>
    public sealed class FakeBrowserTabService : IBrowserTabService
    {
        private readonly Dictionary<WindowHandle, List<string>> _tabs = new Dictionary<WindowHandle, List<string>>();
        private readonly Dictionary<WindowHandle, int> _front = new Dictionary<WindowHandle, int>();

        public Rectangle Page { get; set; } = new Rectangle(0, 120, 1280, 680);

        // Each tab brought to the front, as "window:index".
        public List<string> Selected { get; } = new List<string>();

        public FakeBrowserTabService With(WindowHandle window, int front, params string[] addresses)
        {
            _tabs[window] = addresses.ToList();
            _front[window] = front;
            return this;
        }

        public string? FrontAddress(WindowHandle window)
        {
            if (!_tabs.TryGetValue(window, out List<string>? tabs))
                return null;

            return tabs[_front[window]];
        }

        public Rectangle PageBounds(WindowHandle window)
        {
            return Page;
        }

        public int TabCount(WindowHandle window)
        {
            if (!_tabs.TryGetValue(window, out List<string>? tabs))
                return 0;

            return tabs.Count;
        }

        public int FrontTab(WindowHandle window)
        {
            return _front.GetValueOrDefault(window, -1);
        }

        public bool SelectTab(WindowHandle window, int index)
        {
            if (!_tabs.TryGetValue(window, out List<string>? tabs) || index < 0 || index >= tabs.Count)
                return false;

            _front[window] = index;
            Selected.Add($"{window.Value}:{index}");
            return true;
        }
    }
}
