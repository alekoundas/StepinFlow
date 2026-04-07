namespace Core.Models.Dtos
{
    public class LookupItemDto
    {
        public string Value { get; set; } = string.Empty;     // Stored value
        public string Label { get; set; } = string.Empty;     // Displayed value
        public string? Description { get; set; }              // Optional extra info
        public object? ExtraData { get; set; }                // ExtraData = new { Index = i, IsPrimary = s.Primary, Bounds = s.Bounds }
    }
}
