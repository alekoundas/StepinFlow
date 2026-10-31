using System.Drawing;
using System.Runtime.InteropServices;
using Core.Models.Business;
using Core.Ports;

namespace Platform.Windows.Accessibility
{
    /// <summary>
    /// The element at a point, from Windows' UI Automation: what a screen reader would say about it.
    /// A browser and an ordinary desktop application answer with the element itself - Chromium has
    /// spoken UI Automation natively since Chrome 138 - and an application that draws its own
    /// pixels answers with its window, which says nothing the title does not.
    ///
    /// Asking turns on an application's accessibility tree if nothing had, which a browser builds for
    /// the whole page: the first press of a recording can cost it a moment.
    ///
    /// One automation object, free-threaded, asked from the thread pool. Every property comes through
    /// GetCurrentPropertyValue, so the COM interfaces are declared only as far as the slots used.
    /// </summary>
    public sealed class AccessibleElementService : IAccessibleElementService
    {
        // ================================================================
        // GUIDs and ids
        // ================================================================
        private static readonly Guid CLSID_CUIAUTOMATION8 = new Guid("e22ad333-b25f-460c-83d0-0581107395c9");

        private const int UIA_CONTROL_TYPE_PROPERTY_ID = 30003;
        private const int UIA_NAME_PROPERTY_ID = 30005;
        private const int UIA_IS_ENABLED_PROPERTY_ID = 30010;
        private const int UIA_IS_PASSWORD_PROPERTY_ID = 30019;
        private const int UIA_IS_SELECTION_ITEM_PATTERN_AVAILABLE_PROPERTY_ID = 30036;
        private const int UIA_IS_TOGGLE_PATTERN_AVAILABLE_PROPERTY_ID = 30041;
        private const int UIA_SELECTION_ITEM_IS_SELECTED_PROPERTY_ID = 30079;
        private const int UIA_TOGGLE_STATE_PROPERTY_ID = 30086;
        private const int UIA_IS_DIALOG_PROPERTY_ID = 30174;

        private const int TOGGLE_STATE_ON = 1;

        // Deep enough for a button in a card in a list in a page in a browser.
        private const int MAXIMUM_DEPTH = 24;
        private const int MAXIMUM_INSIDE = 4;

        // UI Automation's control types, by their ids from 50000.
        private static readonly string[] ROLES =
        [
            "button", "calendar", "check box", "combo box", "text field", "link", "image", "list item", "list", "menu",
            "menu bar", "menu item", "progress bar", "radio button", "scroll bar", "slider", "spinner", "status bar", "tab", "tab item",
            "text", "tool bar", "tool tip", "tree", "tree item", "custom", "group", "thumb", "data grid", "data item",
            "document", "split button", "window", "pane", "header", "header item", "table", "title bar", "separator", "semantic zoom",
            "app bar",
        ];

        // What a press is worth saying it sat in.
        private static readonly HashSet<string> CONTAINERS = ["dialog", "window", "document", "group", "list", "list item", "menu", "tool bar", "tab item", "data grid", "table", "tree", "pane"];

        private readonly Lazy<IUIAutomation?> _automation = new Lazy<IUIAutomation?>(Create, LazyThreadSafetyMode.ExecutionAndPublication);


        // ================================================================
        // Public methods
        // ================================================================

        public AccessibleElement? ElementAt(Point point)
        {
            IUIAutomation? automation = _automation.Value;
            if (automation == null)
                return null;

            try
            {
                IUIAutomationElement element = automation.ElementFromPoint(new NativePoint { X = point.X, Y = point.Y });

                return new AccessibleElement
                {
                    Role = RoleOf(element),
                    Name = NameOf(element),
                    IsEnabled = Property(element, UIA_IS_ENABLED_PROPERTY_ID) is not false,
                    IsChecked = CheckedOf(element),
                    IsPassword = Property(element, UIA_IS_PASSWORD_PROPERTY_ID) is true,
                    Inside = ContainersOf(automation, element),
                };
            }
            catch (Exception ex) when (ex is COMException or InvalidCastException)
            {
                // Gone as it was asked about, or an application that does not answer.
                return null;
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static IUIAutomation? Create()
        {
            Type? type = Type.GetTypeFromCLSID(CLSID_CUIAUTOMATION8);
            if (type == null)
                return null;

            return Activator.CreateInstance(type) as IUIAutomation;
        }

        // What it sits in that has a name, nearest first, short of the desktop itself.
        private static List<AccessibleElement> ContainersOf(IUIAutomation automation, IUIAutomationElement element)
        {
            IUIAutomationTreeWalker walker = automation.ControlViewWalker;
            List<AccessibleElement> containers = new List<AccessibleElement>();

            IUIAutomationElement? parent = walker.GetParentElement(element);
            for (int depth = 0; parent != null && depth < MAXIMUM_DEPTH && containers.Count < MAXIMUM_INSIDE; depth++)
            {
                IUIAutomationElement? above = walker.GetParentElement(parent);
                if (above == null)
                    break;

                string role = RoleOf(parent);
                string name = NameOf(parent);
                if (name.Length > 0 && CONTAINERS.Contains(role))
                    containers.Add(new AccessibleElement { Role = role, Name = name });

                parent = above;
            }

            return containers;
        }

        private static string RoleOf(IUIAutomationElement element)
        {
            if (Property(element, UIA_CONTROL_TYPE_PROPERTY_ID) is not int controlType)
                return string.Empty;

            int index = controlType - 50000;
            if (index < 0 || index >= ROLES.Length)
                return string.Empty;

            if (ROLES[index] == "window" && Property(element, UIA_IS_DIALOG_PROPERTY_ID) is true)
                return "dialog";

            return ROLES[index];
        }

        private static string NameOf(IUIAutomationElement element)
        {
            if (Property(element, UIA_NAME_PROPERTY_ID) is not string name)
                return string.Empty;

            return string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        // A check box or a toggle by its state, a radio button by whether it is the one selected. Asked
        // only of an element that has the pattern: anything else answers with the default, unticked.
        private static bool? CheckedOf(IUIAutomationElement element)
        {
            if (Property(element, UIA_IS_TOGGLE_PATTERN_AVAILABLE_PROPERTY_ID) is true && Property(element, UIA_TOGGLE_STATE_PROPERTY_ID) is int state)
                return state == TOGGLE_STATE_ON;

            bool isSelectable = Property(element, UIA_IS_SELECTION_ITEM_PATTERN_AVAILABLE_PROPERTY_ID) is true;
            if (isSelectable && RoleOf(element) == "radio button" && Property(element, UIA_SELECTION_ITEM_IS_SELECTED_PROPERTY_ID) is bool isSelected)
                return isSelected;

            return null;
        }

        // A property of a pattern the element lacks comes back as that property's default.
        private static object? Property(IUIAutomationElement element, int propertyId)
        {
            try
            {
                return element.GetCurrentPropertyValue(propertyId);
            }
            catch (COMException)
            {
                return null;
            }
        }


        // ================================================================
        // COM interface declarations
        // ================================================================
        // From UIAutomationClient.h. A [ComImport] interface is its vtable, slot for slot, so every
        // method before the last one used is declared, and the ones never called take no arguments.

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [ComImport, Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomation
        {
            void CompareElements();
            void CompareRuntimeIds();
            void GetRootElement();
            void ElementFromHandle();
            IUIAutomationElement ElementFromPoint(NativePoint point);
            void GetFocusedElement();
            void GetRootElementBuildCache();
            void ElementFromHandleBuildCache();
            void ElementFromPointBuildCache();
            void GetFocusedElementBuildCache();
            void CreateTreeWalker();
            IUIAutomationTreeWalker ControlViewWalker { get; }
        }

        [ComImport, Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationElement
        {
            void SetFocus();
            void GetRuntimeId();
            void FindFirst();
            void FindAll();
            void FindFirstBuildCache();
            void FindAllBuildCache();
            void BuildUpdatedCache();

            [return: MarshalAs(UnmanagedType.Struct)]
            object GetCurrentPropertyValue(int propertyId);
        }

        [ComImport, Guid("4042c624-389c-4afc-a630-9df854a541fc"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationTreeWalker
        {
            IUIAutomationElement? GetParentElement(IUIAutomationElement element);
        }
    }
}
