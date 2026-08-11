using Core.Enums;
using System.Runtime.InteropServices;

namespace Business.Services.SystemActionService
{
    /// <summary>
    /// Windows does these through an API call. Going through a shell would mean nesting a
    /// DllImport inside a quoted command string, which nobody could edit afterwards.
    /// </summary>
    public sealed class SystemActionService : ISystemActionService
    {
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

        // A plain SendMessage to HWND_BROADCAST waits for every top level window to answer, so one
        // hung application would freeze the flow. The timeout version skips those instead.
        private static void SetMonitorPower(int state) =>
            SendMessageTimeout(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, state,
                SMTO_ABORTIFHUNG, 1000, out _);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam,
            uint flags, uint timeoutMs, out IntPtr result);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool LockWorkStation();

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
    }
}
