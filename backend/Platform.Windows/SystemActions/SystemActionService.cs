using Core.Ports;
using Core.Enums;
using System.Runtime.InteropServices;

namespace Platform.Windows.SystemActions
{
    /// <summary>
    /// Windows does these through an API call.
    /// </summary>
    public sealed partial class SystemActionService : ISystemActionService
    {

        [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
        private static partial IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeoutMs, out IntPtr result);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool LockWorkStation();

        [LibraryImport("powrprof.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetSuspendState(
            [MarshalAs(UnmanagedType.Bool)] bool hibernate,
            [MarshalAs(UnmanagedType.Bool)] bool forceCritical,
            [MarshalAs(UnmanagedType.Bool)] bool disableWakeEvent);


        private const int HWND_BROADCAST = 0xFFFF;
        private const uint WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;

        private const int MONITOR_ON = -1;
        private const int MONITOR_OFF = 2;

        private const uint SMTO_ABORTIFHUNG = 0x0002;


        public void Run(SystemActionTypeEnum action)
        {
            switch (action)
            {
                case SystemActionTypeEnum.LOCK_WORKSTATION:
                    LockWorkStation();
                    break;

                case SystemActionTypeEnum.SLEEP_PC:
                    SetSuspendState(false, true, false);
                    break;

                case SystemActionTypeEnum.MONITOR_OFF:
                    SetMonitorPower(MONITOR_OFF);
                    break;

                case SystemActionTypeEnum.MONITOR_ON:
                    SetMonitorPower(MONITOR_ON);
                    break;
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static void SetMonitorPower(int state)
        {
            SendMessageTimeout(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, state, SMTO_ABORTIFHUNG, 1000, out _);
        }

        
    }
}
