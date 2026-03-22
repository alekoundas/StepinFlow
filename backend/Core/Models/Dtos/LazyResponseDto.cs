namespace Core.Models.Dtos
{
    public class LazyResponseDto
    {
        public int TotalRecords { get; set; }
        public List<FlowDto> Data { get; set; } = new List<FlowDto>();
    }
}
