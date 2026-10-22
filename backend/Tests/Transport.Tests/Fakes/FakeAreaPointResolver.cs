using Business.AreaPoint;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;

namespace Transport.Tests.Fakes
{
    /// <summary>Points by id, already resolved. The resolver has tests of its own.</summary>
    public sealed class FakeAreaPointResolver : IAreaPointResolver
    {
        public Dictionary<int, PointResolution> Points { get; } = new Dictionary<int, PointResolution>();

        public Task<PointResolution> ResolvePointAsync(int flowPointId, CancellationToken ct = default)
        {
            return Task.FromResult(Points.GetValueOrDefault(flowPointId) ?? PointResolution.Fail("The point no longer exists."));
        }

        public Task<AreaResolution> ResolveAreaAsync(int flowAreaId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<PointResolution> ResolvePointAsync(FlowPoint point, CancellationToken ct = default)
        {
            throw new NotImplementedException();
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
