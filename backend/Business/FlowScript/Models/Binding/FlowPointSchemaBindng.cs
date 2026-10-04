using Core.Models.Database;

namespace Business.FlowScript.Models.Binding
{
    public sealed class FlowPointSchemaBindng
    {
        public FlowPoint Point { get; set; } = null!;
        public string? AreaName { get; set; }
        public int Line { get; set; }
    }
}
