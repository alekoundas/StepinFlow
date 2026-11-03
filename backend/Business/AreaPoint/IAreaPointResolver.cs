using Core.Models.Business;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;

namespace Business.AreaPoint
{
    /// <summary>
    /// Turns the stored, portable definition of an area or a point into physical pixels on this
    /// machine. Everything that needs screen coordinates goes through here, so the maths lives
    /// in one place and dry-run reports the same answer execution will get.
    /// </summary>
    public interface IAreaPointResolver
    {
        Task<AreaResolution> ResolveAreaAsync(int flowAreaId, CancellationToken ct = default);
        Task<PointResolution> ResolvePointAsync(int flowPointId, CancellationToken ct = default);

        /// <summary>A point not saved yet, as its form has it, measured from a saved area.</summary>
        Task<PointResolution> ResolvePointAsync(FlowPoint point, CancellationToken ct = default);

        /// <summary>Parent chain must already be loaded.</summary>
        AreaResolution ResolveArea(FlowArea area);

        /// <summary>
        /// A browser tab area's page in this browser window, whatever address its front tab is at
        /// now: where a recording that began on the tab goes on looking.
        /// </summary>
        AreaResolution ResolveTabPage(FlowArea area, WindowHandle window);

        PointResolution ResolvePoint(FlowPoint point);
    }
}
