namespace Core.Models.Dtos
{
    public class LazyRequestDto
    {
        public int Page { get; set; }
        public int Rows { get; set; }
        public string? SortField { get; set; }
        public int? SortOrder { get; set; }
        public Dictionary<string, object>? Filters { get; set; }

        /// <summary>
        /// Flow.getLazy only: which side of the flag to list. The Flows page and the Sub-flows
        /// page are the same query, so neither ever shows the other.
        /// </summary>
        public bool? IsSubFlow { get; set; }
    }
}
