using System.Drawing;
using System.Runtime.InteropServices;
using Core.Models.Business;
using Core.Ports;
using static Platform.Windows.Accessibility.UiAutomation;

namespace Platform.Windows.Accessibility
{
    /// <summary>
    /// A browser window's address bar, tab strip and page, from Windows' UI Automation. Chromium's
    /// browsers - Chrome, Edge, Brave - and Firefox show their tabs as tab items, and their address
    /// bar as the first text field outside the page. A page is never walked into: what is in it is
    /// the site's, and walking it would take as long as the page is big.
    ///
    /// Asking turns on the browser's accessibility tree if nothing had, so a first read can find
    /// less than the next. Each read walks the browser's own controls again, a few hundred at most.
    /// </summary>
    public sealed class BrowserTabService : IBrowserTabService
    {
        private const int UIA_CONTROL_TYPE_PROPERTY_ID = 30003;
        private const int UIA_VALUE_VALUE_PROPERTY_ID = 30045;
        private const int UIA_SELECTION_ITEM_IS_SELECTED_PROPERTY_ID = 30079;

        private const int UIA_SELECTION_ITEM_PATTERN_ID = 10010;
        private const int UIA_LEGACY_IACCESSIBLE_PATTERN_ID = 10018;

        private const int UIA_EDIT_CONTROL_TYPE_ID = 50004;
        private const int UIA_TAB_ITEM_CONTROL_TYPE_ID = 50019;
        private const int UIA_DOCUMENT_CONTROL_TYPE_ID = 50030;

        // Past this it is not a browser's own controls.
        private const int MAXIMUM_ELEMENTS = 3000;


        // ================================================================
        // Public methods
        // ================================================================

        public string? FrontAddress(WindowHandle window)
        {
            return Read(window)?.Address;
        }

        public Rectangle PageBounds(WindowHandle window)
        {
            BrowserParts? parts = Read(window);
            if (parts == null)
                return Rectangle.Empty;

            return parts.Page;
        }

        public int TabCount(WindowHandle window)
        {
            BrowserParts? parts = Read(window);
            if (parts == null)
                return 0;

            return parts.Tabs.Count;
        }

        public int FrontTab(WindowHandle window)
        {
            BrowserParts? parts = Read(window);
            if (parts == null)
                return -1;

            return parts.Tabs.FindIndex(x => Property(x, UIA_SELECTION_ITEM_IS_SELECTED_PROPERTY_ID) is true);
        }

        public bool SelectTab(WindowHandle window, int index)
        {
            BrowserParts? parts = Read(window);
            if (parts == null || index < 0 || index >= parts.Tabs.Count)
                return false;

            return Select(parts.Tabs[index]);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // In the order they are drawn, so a tab's place in the strip is its place in the list: a tab
        // in a group sits deeper than one outside it, and still between its neighbours.
        private static BrowserParts? Read(WindowHandle window)
        {
            IUIAutomation? automation = Automation;
            if (automation == null || !window.IsValid)
                return null;

            try
            {
                IUIAutomationTreeWalker walker = automation.ControlViewWalker;
                BrowserParts parts = new BrowserParts();

                Stack<IUIAutomationElement> waiting = new Stack<IUIAutomationElement>();
                waiting.Push(automation.ElementFromHandle(window.Value));

                for (int seen = 0; waiting.Count > 0 && seen < MAXIMUM_ELEMENTS; seen++)
                {
                    IUIAutomationElement element = waiting.Pop();
                    int controlType = Property(element, UIA_CONTROL_TYPE_PROPERTY_ID) is int type ? type : 0;

                    // The page in front is the one on screen; one switched away is not.
                    if (controlType == UIA_DOCUMENT_CONTROL_TYPE_ID)
                    {
                        Rectangle bounds = VisibleBounds(element);
                        if ((long)bounds.Width * bounds.Height > (long)parts.Page.Width * parts.Page.Height)
                            parts.Page = bounds;
                        continue;
                    }

                    if (controlType == UIA_TAB_ITEM_CONTROL_TYPE_ID)
                        parts.Tabs.Add(element);

                    if (controlType == UIA_EDIT_CONTROL_TYPE_ID && parts.Address == null && Property(element, UIA_VALUE_VALUE_PROPERTY_ID) is string value && value.Length > 0)
                        parts.Address = value;

                    List<IUIAutomationElement> children = new List<IUIAutomationElement>();
                    for (IUIAutomationElement? child = walker.GetFirstChildElement(element); child != null; child = walker.GetNextSiblingElement(child))
                        children.Add(child);

                    for (int i = children.Count - 1; i >= 0; i--)
                        waiting.Push(children[i]);
                }

                return parts;
            }
            catch (Exception ex) when (ex is COMException or InvalidCastException)
            {
                // Closed as it was read, or a window that does not answer.
                return null;
            }
        }

        // A tab item says it can be selected; one that does not still has a default action, which
        // for a tab is switching to it.
        private static bool Select(IUIAutomationElement tab)
        {
            try
            {
                if (tab.GetCurrentPattern(UIA_SELECTION_ITEM_PATTERN_ID) is IUIAutomationSelectionItemPattern selectable)
                {
                    selectable.Select();
                    return true;
                }

                if (tab.GetCurrentPattern(UIA_LEGACY_IACCESSIBLE_PATTERN_ID) is IUIAutomationLegacyIAccessiblePattern legacy)
                {
                    legacy.DoDefaultAction();
                    return true;
                }

                return false;
            }
            catch (Exception ex) when (ex is COMException or InvalidCastException)
            {
                return false;
            }
        }

        private sealed record BrowserParts
        {
            public string? Address { get; set; }
            public Rectangle Page { get; set; } = Rectangle.Empty;
            public List<IUIAutomationElement> Tabs { get; } = new List<IUIAutomationElement>();
        }
    }
}
