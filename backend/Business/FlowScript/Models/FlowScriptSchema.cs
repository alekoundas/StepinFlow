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
        public Flow Flow { get; set; } = new Flow() { PublicId = Guid.Empty };

        public List<FlowViewport> Viewports { get; } = new List<FlowViewport>();
        public List<FlowArea> Areas { get; } = new List<FlowArea>();
        public List<FlowPoint> Points { get; } = new List<FlowPoint>();
        public List<FlowCsvColumn> Inputs { get; } = new List<FlowCsvColumn>();

        /// <summary>In file order, so a parent always comes before its children. Templates are on their step.</summary>
        public List<FlowStep> Steps { get; } = new List<FlowStep>();

        /// <summary>A sub-flow is another flow, which nothing in this file declares, so what each line names is kept for the import to find.</summary>
        public Dictionary<FlowStep, SubFlowReference> SubFlows { get; } = new Dictionary<FlowStep, SubFlowReference>();

        /// <summary>Whether the file had a <c>Kind:</c> line. One from before it keeps what the flow already was.</summary>
        public bool HasKind { get; set; }

        public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

        /// <summary>Nothing fatal. A warning is not a reason to refuse a file.</summary>
        public bool IsValid
        {
            get { return !Diagnostics.Any(x => x.Severity == DiagnosticSeverityEnum.ERROR); }
        }
    }
}
