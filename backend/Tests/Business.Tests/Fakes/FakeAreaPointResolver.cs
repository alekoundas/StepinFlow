using Business.AreaPoint;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// Areas and points by id, already resolved. The resolver has tests of its own; a worker only
    /// needs to know what it gets back.
    /// </summary>
    public sealed class FakeAreaPointResolver : IAreaPointResolver
    {
        public Dictionary<int, AreaResolution> Areas { get; } = new Dictionary<int, AreaResolution>();
        public Dictionary<int, PointResolution> Points { get; } = new Dictionary<int, PointResolution>();

        public Task<AreaResolution> ResolveAreaAsync(int flowAreaId, CancellationToken ct = default)
        {
            return Task.FromResult(Areas.GetValueOrDefault(flowAreaId) ?? AreaResolution.Fail("The area no longer exists."));
        }

        public Task<PointResolution> ResolvePointAsync(int flowPointId, CancellationToken ct = default)
        {
            return Task.FromResult(Points.GetValueOrDefault(flowPointId) ?? PointResolution.Fail("The point no longer exists."));
        }

        public AreaResolution ResolveArea(FlowArea area)
        {
            throw new NotImplementedException();
        }

        public PointResolution ResolvePoint(FlowPoint point)
        {
            throw new NotImplementedException();
        }
    }
}
