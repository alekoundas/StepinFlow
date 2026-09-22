using Core.Models.Database;
using Business.FlowScript.Diagnostics;

namespace Business.FlowScript.Syntax
{
    /// <summary>An area as written, with its parent still a name.</summary>
    public sealed class AreaSyntax
    {
        public FlowArea Area { get; init; } = null!;
        public string? ParentName { get; init; }
        public int Line { get; init; }
    }

    /// <summary>A point as written, with its area still a name.</summary>
    public sealed class PointSyntax
    {
        public FlowPoint Point { get; init; } = null!;
        public string? AreaName { get; init; }
        public int Line { get; init; }
    }

    /// <summary>
    /// A step as written. Everything the script says by name is still a name here - resolving them
    /// needs the whole file read, because a cursor step can aim at a check defined below it.
    /// </summary>
    public sealed class StepSyntax
    {
        public FlowStep Step { get; init; } = null!;
        public int Line { get; init; }

        /// <summary>Index into the document's flat step list. Null for a top level step.</summary>
        public int? ParentIndex { get; set; }

        public string? AreaName { get; set; }
        public string? PointName { get; set; }
        public string? PointEndName { get; set; }
        public string? ReferenceName { get; set; }
        public string? ReferenceEndName { get; set; }
        public string? SubFlowPath { get; set; }

        public List<string> TemplateFileNames { get; } = new List<string>();
    }

    /// <summary>
    /// A flow as a file says it is, before anything has been looked up in the database.
    ///
    /// Flat rather than a tree: a parent is an index, which is what lets the reader build the
    /// shape from indentation in one pass and resolve names in a second.
    /// </summary>
    public sealed class FlowSyntax
    {
        public string FlowName { get; set; } = string.Empty;
        public Guid PublicId { get; set; }

        public List<FlowViewport> Viewports { get; } = new List<FlowViewport>();
        public List<AreaSyntax> Areas { get; } = new List<AreaSyntax>();
        public List<PointSyntax> Points { get; } = new List<PointSyntax>();
        public List<FlowCsvColumn> Inputs { get; } = new List<FlowCsvColumn>();
        public List<StepSyntax> Steps { get; } = new List<StepSyntax>();

        public List<Diagnostic> Diagnostics { get; } = new List<Diagnostic>();

        /// <summary>Nothing fatal. A warning is not a reason to refuse a file.</summary>
        public bool IsValid
        {
            get { return !Diagnostics.Any(x => x.Severity == DiagnosticSeverityEnum.ERROR); }
        }
    }
}
