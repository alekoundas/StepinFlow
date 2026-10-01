using Core.Models.Database;
using Business.FlowScript.Diagnostics;

namespace Business.FlowScript.Models.Binding
{

    /// <summary>
    /// A flow as a file says it is, before anything has been looked up in the database.
    ///
    /// Flat rather than a tree: a parent is an index, which is what lets the reader build the
    /// shape from indentation in one pass and resolve names in a second.
    /// </summary>
    public sealed class FlowScriptSchema
    {
        public string FlowName { get; set; } = string.Empty;
        public Guid PublicId { get; set; }

        public List<FlowViewport> Viewports { get; } = new List<FlowViewport>();
        public List<FlowAreaSchemaBindng> Areas { get; } = new List<FlowAreaSchemaBindng>();
        public List<FlowPointSchemaBindng> Points { get; } = new List<FlowPointSchemaBindng>();
        public List<FlowCsvColumn> Inputs { get; } = new List<FlowCsvColumn>();
        public List<FlowStepTemplateSchemaBindng> Templates { get; } = new List<FlowStepTemplateSchemaBindng>();
        public List<FlowStepSchemaBindng> Steps { get; } = new List<FlowStepSchemaBindng>();

        public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

        /// <summary>Nothing fatal. A warning is not a reason to refuse a file.</summary>
        public bool IsValid
        {
            get { return !Diagnostics.Any(x => x.Severity == DiagnosticSeverityEnum.ERROR); }
        }
    }
}
