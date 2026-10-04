using Business.FlowScript.Models.Text;
using Core.Models.Database;

namespace Business.FlowScript.Models.Binding
{
    public sealed class FlowStepSchemaBindng
    {
        public FlowStep Step { get; set; } = null!;
        public int Line { get; set; }
        public int LeadingSpaces { get; set; }

        /// <summary>Index into the document's flat step list. Null for a top level step.</summary>
        public int? ParentIndex { get; set; }

        public string? AreaName { get; set; }
        public string? PointName { get; set; }
        public string? PointEndName { get; set; }
        public string? ReferenceName { get; set; }
        public string? ReferenceEndName { get; set; }
        public string? SubFlowPath { get; set; }

        public List<ScriptTemplateImage> Templates { get; } = new List<ScriptTemplateImage>();
    }
}
