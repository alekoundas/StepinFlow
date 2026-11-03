using Business.AreaPoint;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Business.AreaPointResolution;
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

        /// <summary>
        /// One answer per lookup, in order, the last kept for every lookup after: a window that
        /// opens, moves or closes while a step waits. Before <see cref="Areas"/>.
        /// </summary>
        public Dictionary<int, Queue<AreaResolution>> AreasInTurn { get; } = new Dictionary<int, Queue<AreaResolution>>();

        public int AreaLookups { get; private set; }

        public Task<AreaResolution> ResolveAreaAsync(int flowAreaId, CancellationToken ct = default)
        {
            AreaLookups++;

            if (AreasInTurn.TryGetValue(flowAreaId, out Queue<AreaResolution>? turns) && turns.Count > 0)
            {
                if (turns.Count > 1)
                    return Task.FromResult(turns.Dequeue());

                return Task.FromResult(turns.Peek());
            }

            return Task.FromResult(Areas.GetValueOrDefault(flowAreaId) ?? AreaResolution.Fail("The area no longer exists.", AreaUnresolvedEnum.NOT_SET_UP));
        }

        public Task<PointResolution> ResolvePointAsync(int flowPointId, CancellationToken ct = default)
        {
            return Task.FromResult(Points.GetValueOrDefault(flowPointId) ?? PointResolution.Fail("The point no longer exists."));
        }

        public Task<PointResolution> ResolvePointAsync(FlowPoint point, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        // An area already loaded, by its name.
        public Dictionary<string, AreaResolution> AreasByName { get; } = new Dictionary<string, AreaResolution>();

        public AreaResolution ResolveArea(FlowArea area)
        {
            return AreasByName.GetValueOrDefault(area.Name) ?? AreaResolution.Fail($"No window matches \"{area.Name}\".", AreaUnresolvedEnum.NOT_ON_SCREEN);
        }

        // A tab's page by its window, whatever address the tab has gone on to.
        public Dictionary<WindowHandle, AreaResolution> TabPages { get; } = new Dictionary<WindowHandle, AreaResolution>();

        public AreaResolution ResolveTabPage(FlowArea area, WindowHandle window)
        {
            return TabPages.GetValueOrDefault(window) ?? AreaResolution.Fail($"\"{area.Name}\"'s window is closed or minimised.", AreaUnresolvedEnum.NOT_ON_SCREEN);
        }

        public PointResolution ResolvePoint(FlowPoint point)
        {
            throw new NotImplementedException();
        }
    }
}
