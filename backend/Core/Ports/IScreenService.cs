using Core.Models.Business;
using System.Drawing;

namespace Core.Ports
{
    // Dependency Inversion Principle(DIP)



    /// <summary>
    /// The monitors attached to the machine, in physical pixels.
    ///
    /// Enabling per-monitor DPI awareness is not here: it runs once at startup before any container
    /// exists, so the adapter exposes it as a static that the composition root calls directly.
    /// </summary>
    public interface IScreenService
    {
        IReadOnlyList<MonitorInfo> GetAllMonitors();

        /// <summary>The rectangle covering every monitor, which is what a capture spans.</summary>
        Rectangle GetVirtualScreenBounds();
    }
}
