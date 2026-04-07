namespace Core.Models.Dtos
{
    public class LookupResponseDto
    {
        public int TotalRecords { get; set; }
        public List<LookupItemDto> Data { get; set; } = new List<LookupItemDto>();
    }
}
