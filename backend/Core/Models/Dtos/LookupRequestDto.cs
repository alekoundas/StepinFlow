namespace Core.Models.Dtos
{
    public class LookupRequestDto
    {
        public string? SearchText { get; set; }          
        //public int? MaxResults { get; set; } = 1000;
        //public bool? OnlyVisible { get; set; }           // e.g. only windows with MainWindowTitle
        public List<int> ExcludedIds { get; set; } = new List<int>();
    }
}
