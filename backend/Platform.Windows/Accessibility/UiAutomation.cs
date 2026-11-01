using System.Drawing;
using System.Runtime.InteropServices;

namespace Platform.Windows.Accessibility
{
    // Windows' UI Automation, as far as this project asks it things: one automation object,
    // free-threaded, shared by everything that reads another application's controls.
    //
    // From UIAutomationClient.h. A [ComImport] interface is its vtable, slot for slot, so every
    // method before the last one used is declared, and the ones never called take no arguments.
    internal static class UiAutomation
    {
        private static readonly Guid CLSID_CUIAUTOMATION8 = new Guid("e22ad333-b25f-460c-83d0-0581107395c9");

        private const int UIA_BOUNDING_RECTANGLE_PROPERTY_ID = 30001;
        private const int UIA_IS_OFFSCREEN_PROPERTY_ID = 30022;

        private static readonly Lazy<IUIAutomation?> _automation = new Lazy<IUIAutomation?>(Create, LazyThreadSafetyMode.ExecutionAndPublication);

        // Null on a Windows without it, which every supported one has.
        public static IUIAutomation? Automation
        {
            get { return _automation.Value; }
        }

        // A property of a pattern the element lacks comes back as that property's default.
        public static object? Property(IUIAutomationElement element, int propertyId)
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

        // Where it is on screen, in physical pixels. Empty when it is scrolled or switched away.
        public static Rectangle VisibleBounds(IUIAutomationElement element)
        {
            if (Property(element, UIA_IS_OFFSCREEN_PROPERTY_ID) is true)
                return Rectangle.Empty;

            if (Property(element, UIA_BOUNDING_RECTANGLE_PROPERTY_ID) is not double[] rectangle || rectangle.Length < 4)
                return Rectangle.Empty;

            return new Rectangle((int)rectangle[0], (int)rectangle[1], (int)rectangle[2], (int)rectangle[3]);
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


        // ================================================================
        // COM interface declarations
        // ================================================================

        [StructLayout(LayoutKind.Sequential)]
        public struct NativePoint
        {
            public int X;
            public int Y;
        }

        [ComImport, Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IUIAutomation
        {
            void CompareElements();
            void CompareRuntimeIds();
            void GetRootElement();
            IUIAutomationElement ElementFromHandle(nint hwnd);
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
        public interface IUIAutomationElement
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

            void GetCurrentPropertyValueEx();
            void GetCachedPropertyValue();
            void GetCachedPropertyValueEx();
            void GetCurrentPatternAs();
            void GetCachedPatternAs();

            [return: MarshalAs(UnmanagedType.IUnknown)]
            object? GetCurrentPattern(int patternId);
        }

        [ComImport, Guid("4042c624-389c-4afc-a630-9df854a541fc"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IUIAutomationTreeWalker
        {
            IUIAutomationElement? GetParentElement(IUIAutomationElement element);
            IUIAutomationElement? GetFirstChildElement(IUIAutomationElement element);
            void GetLastChildElement();
            IUIAutomationElement? GetNextSiblingElement(IUIAutomationElement element);
        }

        [ComImport, Guid("a8efa66a-0fda-421a-9194-38021f3578ea"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IUIAutomationSelectionItemPattern
        {
            void Select();
        }

        [ComImport, Guid("828055ad-355b-4435-86d5-3b51c14a9b1b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IUIAutomationLegacyIAccessiblePattern
        {
            void Select(int flagsSelect);
            void DoDefaultAction();
        }
    }
}
