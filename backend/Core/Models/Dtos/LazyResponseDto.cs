namespace Core.Models.Dtos
{
    public class LazyResponseDto<T>
    {
        public int TotalRecords { get; set; }
        public List<T> Data { get; set; } = new List<T>();
    }
}
