using Business.FlowScript.Diagnostics;
using Core.Models.Database;

namespace Business.FlowScript.Models
{
    /// <summary>
    /// A flow as linked rows: what the scanner reads a file into, what the exporter loads from the
    /// database, and what the printer writes. No ids and no names standing in for rows - a step's
    /// area is <c>step.FlowArea</c>, its parent <c>step.ParentFlowStep</c>.
    /// </summary>
    public sealed class FlowScriptSchema
    {
        /// <summary>The name and the public id. The rows below do not link to it.</summary>
        public Flow Flow { get; set; } = new Flow() { PublicId = Guid.Empty };

        public List<FlowViewport> Viewports { get; } = new List<FlowViewport>();
        public List<FlowArea> Areas { get; } = new List<FlowArea>();
        public List<FlowPoint> Points { get; } = new List<FlowPoint>();
        public List<FlowCsvColumn> Inputs { get; } = new List<FlowCsvColumn>();

        /// <summary>In file order, so a parent always comes before its children. Templates are on their step.</summary>
        public List<FlowStep> Steps { get; } = new List<FlowStep>();

        /// <summary>
        /// A sub-flow is another file, which nothing in this one can link to, so its path is kept as
        /// written.
        /// </summary>
        public Dictionary<FlowStep, string> SubFlowPaths { get; } = new Dictionary<FlowStep, string>();

        public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

        /// <summary>Nothing fatal. A warning is not a reason to refuse a file.</summary>
        public bool IsValid
        {
            get { return !Diagnostics.Any(x => x.Severity == DiagnosticSeverityEnum.ERROR); }
        }
    }
}
