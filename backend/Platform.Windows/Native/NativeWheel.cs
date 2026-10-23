using System.Runtime.InteropServices;

namespace Platform.Windows.Native
{
    /// <summary>
    /// The wheel settings from the Windows mouse options: how many lines, or characters sideways,
    /// one notch scrolls.
    /// </summary>
    internal static partial class NativeWheel
    {
        private const uint SPI_GETWHEELSCROLLLINES = 0x0068;
        private const uint SPI_GETWHEELSCROLLCHARS = 0x006C;

        [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SystemParametersInfo(uint uiAction, uint uiParam, out uint pvParam, uint fWinIni);


        //==================================================
        // Public methods
        //==================================================

        public static uint ScrollPerNotch(bool isHorizontal)
        {
            uint action = isHorizontal ? SPI_GETWHEELSCROLLCHARS : SPI_GETWHEELSCROLLLINES;

            if (!SystemParametersInfo(action, 0, out uint amount, 0))
                return 1;

            return amount;
        }
    }
}
