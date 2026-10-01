using Core.Models.Database;

namespace Business.FlowScript.Models.Binding
{
    public sealed class FlowAreaSchemaBindng
    {
        public FlowArea Area { get; init; } = null!;
        public string? ParentName { get; init; }
        public int Line { get; init; }
    }
}
