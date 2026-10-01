using Core.Models.Database;

namespace Business.FlowScript.Models.Binding
{
    public sealed class FlowPointSchemaBindng
    {
        public FlowPoint Point { get; init; } = null!;
        public string? AreaName { get; init; }
        public int Line { get; init; }
    }
}
