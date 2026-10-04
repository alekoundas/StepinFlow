using Core.Models.Database;

namespace Business.FlowScript.Models.Binding
{
    public sealed class FlowAreaSchemaBindng
    {
        public FlowArea Area { get; set; } = null!;
        public string? ParentName { get; set; }
        public int Line { get; set; }
    }
}
