using Core.Models.Business;

namespace Core.Helpers
{
    /// <summary>
    /// Which monitor a MONITOR area means. An empty name is the primary monitor - the one choice
    /// that means the same thing on another PC. The resolver and the capture both ask here, so
    /// they cannot come to different answers.
    /// </summary>
    public static class MonitorHelper
    {
        public static MonitorInfo? Find(IEnumerable<MonitorInfo> monitors, string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return monitors.FirstOrDefault(x => x.IsPrimary);

            return monitors.FirstOrDefault(x => string.Equals(x.DeviceId, deviceName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
