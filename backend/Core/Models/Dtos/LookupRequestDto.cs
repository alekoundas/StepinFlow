using Core.Enums;

namespace Core.Models.Dtos
{
    public class LookupRequestDto
    {
        public string? SearchText { get; set; }
        //public int? MaxResults { get; set; } = 1000;
        //public bool? OnlyVisible { get; set; }           // e.g. only windows with MainWindowTitle
        public List<int> ExcludedIds { get; set; } = new List<int>();

        // Scope filters. Lookup.flowLocation uses FlowId, Lookup.flowStep walks up from FlowStepId.
        public int? FlowId { get; set; }
        public int? FlowStepId { get; set; }

        // Lookup.flowSearchArea only: window steps want APPLICATION areas, not monitors.
        public FlowSearchAreaTypeEnum? FlowSearchAreaType { get; set; }
    }
}
