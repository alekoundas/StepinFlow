using Business.FlowScript.Models.Text;
using Core.Models.Database;

namespace Business.FlowScript.Binding
{
    /// <summary>
    /// Everything the writer needs, arranged once.
    ///
    /// Names rather than ids, because the script refers to a step, an area and a point by name
    /// </summary>
    public class BoundFlow
    {
        public Flow Flow { get; set; } = null!;

        public IReadOnlyList<FlowArea> Areas { get; set; } = [];
        public IReadOnlyList<FlowPoint> Points { get; set; } = [];
        public IReadOnlyList<FlowCsvColumn> Inputs { get; set; } = [];
        public IReadOnlyList<FlowViewport> Viewports { get; set; } = [];

        public IReadOnlyList<FlowStep> Steps { get; set; } = [];

        public IReadOnlyDictionary<int, string> AreaNamesById { get; set; } = new Dictionary<int, string>();
        public IReadOnlyDictionary<int, string> PointNamesById { get; set; } = new Dictionary<int, string>();
        public IReadOnlyDictionary<int, string> StepNamesById { get; set; } = new Dictionary<int, string>();
        public IReadOnlyDictionary<int, string> SubFlowPathsById { get; set; } = new Dictionary<int, string>();
        public IReadOnlyDictionary<int, IReadOnlyList<ScriptTemplateImage>> TemplatesByStepId { get; set; } = new Dictionary<int, IReadOnlyList<ScriptTemplateImage>>();

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
