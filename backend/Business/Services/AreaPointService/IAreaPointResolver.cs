using Core.Models.Business;
using Core.Models.Database;

namespace Business.Services.AreaPointService
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

        /// <summary>Parent chain must already be loaded.</summary>
        AreaResolution ResolveArea(FlowArea area);
        PointResolution ResolvePoint(FlowPoint point);
    }
}
