namespace Core.Models.Dtos
{
    public class DataTableRequestDto
    {
        public int Page { get; set; }
        public int Rows { get; set; }
        public string? SortField { get; set; }
        public int? SortOrder { get; set; }
        public Dictionary<string, object>? Filters { get; set; }
    }
}
