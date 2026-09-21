using Core.Models.Database;

namespace Business.Services.FlowScriptService
{
    /// <summary>Where a file stopped making sense, and what was expected instead.</summary>
    public sealed record FlowScriptError(int Line, int Column, string Message);

    /// <summary>An area as written, with its parent still a name.</summary>
    public sealed class ParsedArea
    {
        public FlowArea Area { get; init; } = null!;
        public string? ParentName { get; init; }
        public int Line { get; init; }
    }

    /// <summary>A point as written, with its area still a name.</summary>
    public sealed class ParsedPoint
    {
        public FlowPoint Point { get; init; } = null!;
        public string? AreaName { get; init; }
        public int Line { get; init; }
    }

    /// <summary>
    /// A step as written. Everything the script says by name is still a name here - resolving them
    /// needs the whole file read, because a cursor step can aim at a check defined below it.
    /// </summary>
    public sealed class ParsedStep
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
    public sealed class FlowScriptDocument
    {
        public string FlowName { get; set; } = string.Empty;
        public Guid PublicId { get; set; }

        public List<FlowViewport> Viewports { get; } = new List<FlowViewport>();
        public List<ParsedArea> Areas { get; } = new List<ParsedArea>();
        public List<ParsedPoint> Points { get; } = new List<ParsedPoint>();
        public List<FlowCsvColumn> Inputs { get; } = new List<FlowCsvColumn>();
        public List<ParsedStep> Steps { get; } = new List<ParsedStep>();

        public List<FlowScriptError> Errors { get; } = new List<FlowScriptError>();

        public bool IsValid
        {
            get { return Errors.Count == 0; }
        }
    }
}
