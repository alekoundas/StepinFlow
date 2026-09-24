using Business.FlowScript.Syntax;

using Core.Models.Database;

namespace Business.FlowScript.Binding
{
    /// <summary>
    /// Everything the writer needs, arranged once.
    ///
    /// Names rather than ids, because the script refers to a step, an area and a point by name -
    /// which is why phase 1 made them unique.
    /// </summary>
    public class BoundFlow
    {
        public Flow Flow { get; set; } = null!;

        public IReadOnlyList<FlowArea> Areas { get; set; } = [];
        public IReadOnlyList<FlowPoint> Points { get; set; } = [];
        public IReadOnlyList<FlowCsvColumn> Inputs { get; set; } = [];
        public IReadOnlyList<FlowViewport> Viewports { get; set; } = [];

        /// <summary>Every step including the branch rows, which the walk goes through.</summary>
        public IReadOnlyList<FlowStep> Steps { get; set; } = [];

        public IReadOnlyDictionary<int, string> AreaNamesById { get; set; } = new Dictionary<int, string>();
        public IReadOnlyDictionary<int, string> PointNamesById { get; set; } = new Dictionary<int, string>();
        public IReadOnlyDictionary<int, string> StepNamesById { get; set; } = new Dictionary<int, string>();

        /// <summary>The templates each step names: the file each was written to, and its accuracy.</summary>
        public IReadOnlyDictionary<int, IReadOnlyList<ScriptTemplate>> TemplatesByStepId { get; set; } = new Dictionary<int, IReadOnlyList<ScriptTemplate>>();

        /// <summary>A sub-flow's path relative to the repository root.</summary>
        public IReadOnlyDictionary<int, string> SubFlowPathsById { get; set; } = new Dictionary<int, string>();

        private ILookup<int?, FlowStep>? _childrenByParent;

        /// <summary>
        /// A step's children in running order. Built on first use rather than by the caller, so the
        /// writer cannot be handed a source that orders them by chance.
        /// </summary>
        public IEnumerable<FlowStep> ChildrenOf(int? parentFlowStepId)
        {
            _childrenByParent ??= Steps.ToLookup(x => x.ParentFlowStepId);

            return _childrenByParent[parentFlowStepId].OrderBy(x => x.OrderNumber);
        }
    }
}
